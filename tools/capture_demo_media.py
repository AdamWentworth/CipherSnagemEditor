#!/usr/bin/env python3
"""Capture Phlosion-ready Cipher Snagem Editor demo media from a local ISO."""

from __future__ import annotations

import argparse
import ctypes
import json
import os
import re
import shutil
import signal
import struct
import subprocess
import sys
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

from PIL import Image, ImageChops


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT_DIR = REPO_ROOT / "artifacts" / "demo-media" / "cipher-snagem-editor"
DEFAULT_ISO_CANDIDATES = [
    Path("/home/adam/Pokemon Colosseum/Pokemon Colosseum.iso"),
    REPO_ROOT / ".local" / "Pokemon Colosseum.iso",
]
DEFAULT_APP_CANDIDATES = [
    REPO_ROOT / ".local" / "cipher-package" / "opt" / "cipher-snagem-editor" / "CipherSnagemEditor.App",
    Path("/opt/cipher-snagem-editor/CipherSnagemEditor.App"),
]

CAPTURE_X = 80
CAPTURE_Y = 50
FRAME_RATE = 30


@dataclass(frozen=True)
class ToolSpec:
    title: str
    row_index: int


@dataclass(frozen=True)
class TrainerShowcaseTarget:
    label: str
    search_text: str
    click_path_y: tuple[int, ...]


TOOLS = {
    "trainer": ToolSpec("Trainer Editor", 0),
    "stats": ToolSpec("Pokemon Stats Editor", 1),
    "move": ToolSpec("Move Editor", 2),
    "item": ToolSpec("Item Editor", 3),
    "randomizer": ToolSpec("Randomizer", 8),
    "iso": ToolSpec("ISO Explorer", 14),
}

SCREENSHOTS = [
    ("workspace", None),
    ("trainer-editor", "trainer"),
    ("pokemon-stats", "stats"),
    ("move-editor", "move"),
]

SHADOW_STARTER_TRAINERS = [
    # Avalonia refreshes the selected trainer while typing, so these short prefixes
    # plus row paths are the reliable route to the populated starter-shadow battles.
    TrainerShowcaseTarget("Bluno", "blu", (82, 132, 182, 82)),
    TrainerShowcaseTarget("Verde", "ver", (82, 132)),
    TrainerShowcaseTarget("Rosso", "ros", (82, 132, 182, 232)),
]

VIDEOS = [
    ("workspace-loaded", None, None),
    ("pokemon-stats", "stats", None),
    ("move-editor", "move", None),
    ("trainer-verde", "trainer", SHADOW_STARTER_TRAINERS[1]),
    ("trainer-rosso", "trainer", SHADOW_STARTER_TRAINERS[2]),
    ("trainer-bluno", "trainer", SHADOW_STARTER_TRAINERS[0]),
]


class X11Driver:
    def __init__(self) -> None:
        self.x11 = ctypes.cdll.LoadLibrary("libX11.so.6")
        self.xtst = ctypes.cdll.LoadLibrary("libXtst.so.6")

        self.x11.XOpenDisplay.restype = ctypes.c_void_p
        self.x11.XCloseDisplay.argtypes = [ctypes.c_void_p]
        self.x11.XMoveResizeWindow.argtypes = [
            ctypes.c_void_p,
            ctypes.c_ulong,
            ctypes.c_int,
            ctypes.c_int,
            ctypes.c_uint,
            ctypes.c_uint,
        ]
        self.x11.XRaiseWindow.argtypes = [ctypes.c_void_p, ctypes.c_ulong]
        self.x11.XKeysymToKeycode.argtypes = [ctypes.c_void_p, ctypes.c_ulong]
        self.x11.XKeysymToKeycode.restype = ctypes.c_uint
        self.x11.XFlush.argtypes = [ctypes.c_void_p]
        self.xtst.XTestFakeMotionEvent.argtypes = [
            ctypes.c_void_p,
            ctypes.c_int,
            ctypes.c_int,
            ctypes.c_int,
            ctypes.c_ulong,
        ]
        self.xtst.XTestFakeButtonEvent.argtypes = [ctypes.c_void_p, ctypes.c_uint, ctypes.c_bool, ctypes.c_ulong]
        self.xtst.XTestFakeKeyEvent.argtypes = [ctypes.c_void_p, ctypes.c_uint, ctypes.c_bool, ctypes.c_ulong]

        self.display = self.x11.XOpenDisplay(None)
        if not self.display:
            raise RuntimeError("Could not open the X11 display. Run this from an active desktop session.")

    def close(self) -> None:
        if self.display:
            self.x11.XCloseDisplay(self.display)
            self.display = None

    def flush(self) -> None:
        self.x11.XFlush(self.display)

    def move_resize(self, window_id: int, width: int, height: int, x: int = CAPTURE_X, y: int = CAPTURE_Y) -> None:
        self.x11.XMoveResizeWindow(self.display, window_id, x, y, width, height)
        self.x11.XRaiseWindow(self.display, window_id)
        self.flush()
        time.sleep(0.25)

    def move_window(self, window_id: int, x: int = CAPTURE_X, y: int = CAPTURE_Y) -> None:
        width, height = window_size(window_id)
        self.move_resize(window_id, width, height, x, y)

    def raise_window(self, window_id: int) -> None:
        self.x11.XRaiseWindow(self.display, window_id)
        self.flush()
        time.sleep(0.15)

    def move(self, x: int, y: int) -> None:
        self.xtst.XTestFakeMotionEvent(self.display, -1, x, y, 0)
        self.flush()

    def move_smooth(self, start: tuple[int, int], end: tuple[int, int], duration: float = 0.45, steps: int = 24) -> None:
        start_x, start_y = start
        end_x, end_y = end
        for step in range(steps + 1):
            progress = step / steps
            eased = progress * progress * (3 - 2 * progress)
            x = round(start_x + (end_x - start_x) * eased)
            y = round(start_y + (end_y - start_y) * eased)
            self.move(x, y)
            time.sleep(duration / steps)

    def click(self, x: int, y: int) -> None:
        self.move(x, y)
        time.sleep(0.12)
        self.xtst.XTestFakeButtonEvent(self.display, 1, True, 0)
        self.xtst.XTestFakeButtonEvent(self.display, 1, False, 0)
        self.flush()
        time.sleep(0.35)

    def double_click(self, x: int, y: int) -> None:
        self.move(x, y)
        time.sleep(0.12)
        for _ in range(2):
            self.xtst.XTestFakeButtonEvent(self.display, 1, True, 0)
            self.flush()
            time.sleep(0.035)
            self.xtst.XTestFakeButtonEvent(self.display, 1, False, 0)
            self.flush()
            time.sleep(0.08)
        time.sleep(0.35)

    def wheel(self, x: int, y: int, direction: str, notches: int = 4) -> None:
        button = 4 if direction == "up" else 5
        self.move(x, y)
        for _ in range(notches):
            self.xtst.XTestFakeButtonEvent(self.display, button, True, 0)
            self.xtst.XTestFakeButtonEvent(self.display, button, False, 0)
            self.flush()
            time.sleep(0.07)

    def key_code(self, keysym: int) -> int:
        keycode = self.x11.XKeysymToKeycode(self.display, keysym)
        if keycode == 0:
            raise RuntimeError(f"Could not resolve X11 keysym: {keysym:#x}")
        return keycode

    def key_event(self, keysym: int, pressed: bool) -> None:
        self.xtst.XTestFakeKeyEvent(self.display, self.key_code(keysym), pressed, 0)
        self.flush()
        time.sleep(0.025)

    def press_key(self, keysym: int) -> None:
        self.key_event(keysym, True)
        self.key_event(keysym, False)

    def press_chord(self, modifier_keysym: int, key_keysym: int) -> None:
        self.key_event(modifier_keysym, True)
        self.key_event(key_keysym, True)
        self.key_event(key_keysym, False)
        self.key_event(modifier_keysym, False)

    def type_text(self, text: str) -> None:
        for character in text:
            if character == " ":
                keysym = 0x20
            else:
                keysym = ord(character.lower())
            self.press_key(keysym)
            time.sleep(0.035)

    def type_text_with_refocus(self, text: str, focus_point: tuple[int, int]) -> None:
        for character in text:
            self.click(*focus_point)
            self.type_text(character)
            time.sleep(0.18)


class RunningApp:
    def __init__(self, app_path: Path, iso_path: Path, log_path: Path) -> None:
        self.app_path = app_path
        self.iso_path = iso_path
        self.log_path = log_path
        self.process: subprocess.Popen[bytes] | None = None

    def __enter__(self) -> "RunningApp":
        self.log_path.parent.mkdir(parents=True, exist_ok=True)
        log_file = self.log_path.open("ab")
        self.process = subprocess.Popen(
            [str(self.app_path), str(self.iso_path)],
            cwd=self.app_path.parent,
            stdin=subprocess.DEVNULL,
            stdout=log_file,
            stderr=subprocess.STDOUT,
            start_new_session=True,
        )
        return self

    def __exit__(self, exc_type, exc, traceback) -> None:  # noqa: ANN001
        if self.process is None:
            return

        if self.process.poll() is None:
            os.killpg(self.process.pid, signal.SIGTERM)
            try:
                self.process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                os.killpg(self.process.pid, signal.SIGKILL)
                self.process.wait(timeout=5)


def require_command(name: str) -> None:
    if shutil.which(name) is None:
        raise RuntimeError(f"Required command not found on PATH: {name}")


def resolve_first(explicit: str | None, env_name: str, candidates: list[Path], description: str) -> Path:
    if explicit:
        path = Path(explicit).expanduser()
        if path.exists():
            return path.resolve()
        raise FileNotFoundError(f"{description} does not exist: {path}")

    env_value = os.environ.get(env_name)
    if env_value:
        path = Path(env_value).expanduser()
        if path.exists():
            return path.resolve()
        raise FileNotFoundError(f"{env_name} points to a missing {description}: {path}")

    for candidate in candidates:
        if candidate.exists():
            return candidate.resolve()

    formatted = "\n".join(f"  - {candidate}" for candidate in candidates)
    raise FileNotFoundError(f"Could not find {description}. Tried:\n{formatted}")


def xwininfo_tree() -> str:
    return subprocess.check_output(["xwininfo", "-root", "-tree"], text=True)


def find_window_id(title: str) -> int | None:
    title_re = re.compile(r"^\s*(0x[0-9a-f]+)\s+\"([^\"]+)\"", re.IGNORECASE)
    for line in xwininfo_tree().splitlines():
        match = title_re.match(line)
        if match and title in match.group(2):
            return int(match.group(1), 16)
    return None


def wait_for_window(title: str, timeout: float = 20.0) -> int:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        window_id = find_window_id(title)
        if window_id is not None:
            return window_id
        time.sleep(0.2)
    raise TimeoutError(f"Timed out waiting for window: {title}")


def window_size(window_id: int) -> tuple[int, int]:
    output = subprocess.check_output(["xwininfo", "-id", hex(window_id)], text=True)
    width_match = re.search(r"^\s*Width:\s*(\d+)", output, re.MULTILINE)
    height_match = re.search(r"^\s*Height:\s*(\d+)", output, re.MULTILINE)
    if not width_match or not height_match:
        raise RuntimeError(f"Could not read window size for {hex(window_id)}")
    return int(width_match.group(1)), int(height_match.group(1))


def window_geometry(window_id: int) -> tuple[int, int, int, int]:
    output = subprocess.check_output(["xwininfo", "-id", hex(window_id)], text=True)
    x_match = re.search(r"^\s*Absolute upper-left X:\s*(-?\d+)", output, re.MULTILINE)
    y_match = re.search(r"^\s*Absolute upper-left Y:\s*(-?\d+)", output, re.MULTILINE)
    width_match = re.search(r"^\s*Width:\s*(\d+)", output, re.MULTILINE)
    height_match = re.search(r"^\s*Height:\s*(\d+)", output, re.MULTILINE)
    if not x_match or not y_match or not width_match or not height_match:
        raise RuntimeError(f"Could not read window geometry for {hex(window_id)}")
    return (
        int(x_match.group(1)),
        int(y_match.group(1)),
        int(width_match.group(1)),
        int(height_match.group(1)),
    )


def window_point(window_id: int, x: int, y: int) -> tuple[int, int]:
    window_x, window_y, _, _ = window_geometry(window_id)
    return window_x + x, window_y + y


def decode_xwd(path: Path) -> Image.Image:
    data = path.read_bytes()
    if len(data) < 100:
        raise ValueError(f"XWD file is too small: {path}")

    values = struct.unpack(">25I", data[:100])
    header_size = values[0]
    file_version = values[1]
    width = values[4]
    height = values[5]
    bits_per_pixel = values[11]
    bytes_per_line = values[12]
    color_count = values[19]

    if file_version != 7 or bits_per_pixel != 32:
        raise ValueError(f"Unsupported XWD format in {path}: version={file_version}, bpp={bits_per_pixel}")

    offset = header_size + color_count * 12
    raw = data[offset : offset + height * bytes_per_line]
    return Image.frombytes("RGBA", (width, height), raw, "raw", "BGRA", bytes_per_line, 1)


def capture_window_png(window_id: int, output_path: Path, scratch_dir: Path) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    if output_path.exists():
        output_path.unlink()

    command = [
        "gst-launch-1.0",
        "-q",
        "ximagesrc",
        "use-damage=0",
        "show-pointer=false",
        f"xid={hex(window_id)}",
        "num-buffers=1",
        "!",
        "video/x-raw,framerate=1/1",
        "!",
        "videoconvert",
        "!",
        "pngenc",
        "!",
        "filesink",
        f"location={output_path}",
    ]

    last_error: subprocess.CalledProcessError | None = None
    for _ in range(4):
        time.sleep(0.35)
        try:
            subprocess.run(command, check=True)
            return
        except subprocess.CalledProcessError as exc:
            last_error = exc
            time.sleep(0.8)

    if last_error is not None:
        raise last_error


def capture_window_image(window_id: int, scratch_dir: Path) -> Image.Image:
    scratch_dir.mkdir(parents=True, exist_ok=True)
    xwd_path = scratch_dir / f"window-{window_id:x}-{time.monotonic_ns()}.xwd"
    try:
        subprocess.run(
            ["xwd", "-silent", "-id", hex(window_id), "-out", str(xwd_path)],
            check=True,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
        return decode_xwd(xwd_path).convert("RGB")
    finally:
        xwd_path.unlink(missing_ok=True)


def capture_stats_detail_crop(window_id: int, scratch_dir: Path) -> Image.Image:
    image = capture_window_image(window_id, scratch_dir)
    return image.crop((288, 12, 438, 102))


def detail_crop_change_ratio(previous: Image.Image, current: Image.Image) -> float:
    diff = ImageChops.difference(previous, current)
    changed_pixels = 0
    for red, green, blue in diff.getdata():
        if red + green + blue > 36:
            changed_pixels += 1

    return changed_pixels / (diff.width * diff.height)


def wait_for_stats_detail_change(
    window_id: int,
    previous_crop: Image.Image,
    scratch_dir: Path,
    *,
    timeout: float = 4.0,
) -> Image.Image:
    deadline = time.monotonic() + timeout
    latest_crop = previous_crop
    while time.monotonic() < deadline:
        time.sleep(0.12)
        latest_crop = capture_stats_detail_crop(window_id, scratch_dir)
        if detail_crop_change_ratio(previous_crop, latest_crop) > 0.015:
            return latest_crop

    return latest_crop


def main_tool_click(main_window: int, row_index: int) -> tuple[int, int]:
    return window_point(main_window, 100, 24 + row_index * 50 + 25)


def open_tool(driver: X11Driver, main_window: int, tool_key: str) -> int:
    tool = TOOLS[tool_key]
    title = f"{tool.title} - Colosseum Tool"
    window = find_window_id(title)
    for _ in range(5):
        if window is not None:
            break
        driver.raise_window(main_window)
        driver.click(*main_tool_click(main_window, tool.row_index))
        time.sleep(0.8)
        window = find_window_id(title)

    if window is None:
        window = wait_for_window(title, timeout=5)

    return window


def activate_window(driver: X11Driver, window_id: int, cursor_point: tuple[int, int] | None = None) -> None:
    driver.raise_window(window_id)
    driver.click(*window_point(window_id, 455, 14))
    if cursor_point is not None:
        driver.move(*cursor_point)
    time.sleep(0.35)


def select_trainer_showcase_target(
    driver: X11Driver,
    window_id: int,
    target: TrainerShowcaseTarget,
    settle_seconds: float = 1.2,
) -> None:
    search_focus_point = window_point(window_id, 228, 44)
    driver.click(*search_focus_point)
    driver.press_chord(0xFFE3, ord("a"))  # Control_L + A
    driver.press_key(0xFF08)  # Backspace
    driver.type_text_with_refocus(target.search_text, search_focus_point)
    time.sleep(1.4)
    for row_y in target.click_path_y:
        row_point = window_point(window_id, 145, row_y)
        driver.move(*row_point)
        time.sleep(0.2)
        driver.click(*row_point)
        time.sleep(0.55)
    time.sleep(settle_seconds)


def prepare_tool(driver: X11Driver, tool_key: str, window_id: int) -> None:
    if tool_key == "trainer":
        select_trainer_showcase_target(driver, window_id, SHADOW_STARTER_TRAINERS[0])
    elif tool_key == "stats":
        # The stats editor populates its list asynchronously; selecting too early
        # leaves the default blank row in the detail pane.
        time.sleep(2.2)
        activate_window(driver, window_id)
        driver.click(*window_point(window_id, 112, 138))
        time.sleep(1.4)
    elif tool_key == "move":
        driver.click(*window_point(window_id, 105, 160))
    elif tool_key == "item":
        driver.click(*window_point(window_id, 105, 140))
    elif tool_key == "iso":
        driver.wheel(*window_point(window_id, 250, 310), "down", 3)
    elif tool_key == "randomizer":
        driver.click(*window_point(window_id, 82, 84))
        driver.click(*window_point(window_id, 84, 244))
    driver.raise_window(window_id)
    time.sleep(2.0)


def start_recording(output_path: Path, window_id: int) -> subprocess.Popen[bytes]:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    if output_path.exists():
        output_path.unlink()

    width, height = window_size(window_id)
    crop_right = width % 2
    crop_bottom = height % 2

    pipeline = [
        "gst-launch-1.0",
        "-e",
        "-q",
        "ximagesrc",
        "use-damage=0",
        "show-pointer=true",
        f"xid={hex(window_id)}",
        "!",
        f"video/x-raw,framerate={FRAME_RATE}/1",
        "!",
    ]
    if crop_right or crop_bottom:
        pipeline.extend(
            [
                "videocrop",
                f"right={crop_right}",
                f"bottom={crop_bottom}",
                "!",
            ]
        )

    pipeline.extend(
        [
        "videoconvert",
        "!",
        "x264enc",
        "tune=zerolatency",
        "speed-preset=veryfast",
        "bitrate=9000",
        "key-int-max=30",
        "!",
        "video/x-h264,profile=high",
        "!",
        "mp4mux",
        "!",
        "filesink",
        f"location={output_path}",
        ]
    )
    return subprocess.Popen(pipeline, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def stop_recording(process: subprocess.Popen[bytes]) -> None:
    if process.poll() is None:
        process.send_signal(signal.SIGINT)
        try:
            process.wait(timeout=8)
        except subprocess.TimeoutExpired:
            process.terminate()
            process.wait(timeout=5)


def record_video(output_path: Path, window_id: int, action: Callable[[], None], pre_roll_seconds: float = 1.5) -> None:
    recorder = start_recording(output_path, window_id)
    time.sleep(pre_roll_seconds)
    try:
        action()
        time.sleep(1.0)
    finally:
        stop_recording(recorder)

    if not output_path.exists() or output_path.stat().st_size == 0:
        raise RuntimeError(f"Video capture failed: {output_path}")


def record_tool_flow(
    driver: X11Driver,
    tool_key: str | None,
    window_id: int,
    output_path: Path,
    trainer_target: TrainerShowcaseTarget | None = None,
) -> None:
    scratch_dir = output_path.parent.parent / ".scratch-video"
    driver.raise_window(window_id)
    time.sleep(0.8)
    start_point = None
    if tool_key == "stats":
        start_point = window_point(window_id, 612, 86)
    activate_window(driver, window_id, start_point)

    def action() -> None:
        driver.raise_window(window_id)
        if tool_key is None:
            top_tool = window_point(window_id, 102, 50)
            stats_tool = window_point(window_id, 102, 100)
            move_tool = window_point(window_id, 102, 150)
            item_tool = window_point(window_id, 102, 200)
            log_area = window_point(window_id, 420, 92)
            driver.move_smooth(log_area, top_tool, duration=0.55)
            time.sleep(0.8)
            driver.move_smooth(top_tool, stats_tool, duration=0.45)
            time.sleep(0.5)
            driver.move_smooth(stats_tool, move_tool, duration=0.45)
            time.sleep(0.5)
            driver.move_smooth(move_tool, item_tool, duration=0.45)
            time.sleep(0.6)
            driver.wheel(*window_point(window_id, 112, 410), "down", 5)
            time.sleep(0.9)
            driver.wheel(*window_point(window_id, 112, 410), "up", 5)
            time.sleep(0.9)
            driver.move_smooth(item_tool, log_area, duration=0.55)
            time.sleep(1.2)
        elif tool_key == "trainer":
            if trainer_target is not None and trainer_target.label == "Bluno":
                selected_row = window_point(window_id, 145, 82)
                battle_id_header = window_point(window_id, 995, 47)
                driver.move_smooth(selected_row, battle_id_header, duration=0.75)
                time.sleep(1.2)
            driver.move(*window_point(window_id, 310, 170))
            time.sleep(1.8)
            driver.move(*window_point(window_id, 765, 170))
            time.sleep(1.8)
            driver.move(*window_point(window_id, 1200, 170))
            time.sleep(1.8)
            driver.move(*window_point(window_id, 820, 735))
            time.sleep(1.3)
        elif tool_key == "stats":
            charmander = window_point(window_id, 112, 288)
            squirtle = window_point(window_id, 112, 438)
            type_panel = window_point(window_id, 318, 282)
            move_panel = window_point(window_id, 612, 86)
            evolution_panel = window_point(window_id, 790, 282)

            def select_stats_row(row: tuple[int, int]) -> None:
                driver.click(*row)
                time.sleep(0.18)

            detail_crop = capture_stats_detail_crop(window_id, scratch_dir)

            driver.move_smooth(move_panel, charmander, duration=0.5)
            select_stats_row(charmander)
            detail_crop = wait_for_stats_detail_change(window_id, detail_crop, scratch_dir)
            time.sleep(0.35)
            driver.move_smooth(charmander, squirtle, duration=0.42)
            select_stats_row(squirtle)
            detail_crop = wait_for_stats_detail_change(window_id, detail_crop, scratch_dir)
            time.sleep(0.45)
            driver.move_smooth(squirtle, type_panel, duration=0.45)
            time.sleep(0.55)
            driver.move_smooth(type_panel, move_panel, duration=0.45)
            time.sleep(0.55)
            driver.move_smooth(move_panel, evolution_panel, duration=0.55)
            time.sleep(0.8)
            driver.wheel(*window_point(window_id, 118, 452), "down", 5)
            time.sleep(0.9)
            driver.wheel(*window_point(window_id, 118, 452), "up", 5)
            time.sleep(0.8)
        elif tool_key == "move":
            pound = window_point(window_id, 104, 138)
            karate_chop = window_point(window_id, 104, 188)
            fire_punch = window_point(window_id, 104, 438)
            ice_punch = window_point(window_id, 104, 488)
            type_panel = window_point(window_id, 320, 232)
            power_panel = window_point(window_id, 535, 228)
            flag_panel = window_point(window_id, 768, 368)
            description_panel = window_point(window_id, 672, 82)
            driver.move_smooth(description_panel, pound, duration=0.45)
            driver.click(*pound)
            time.sleep(0.8)
            driver.move_smooth(pound, type_panel, duration=0.5)
            time.sleep(0.7)
            driver.move_smooth(type_panel, karate_chop, duration=0.55)
            driver.click(*karate_chop)
            time.sleep(0.8)
            driver.move_smooth(karate_chop, power_panel, duration=0.55)
            time.sleep(0.7)
            driver.move_smooth(power_panel, fire_punch, duration=0.65)
            driver.click(*fire_punch)
            time.sleep(0.9)
            driver.move_smooth(fire_punch, flag_panel, duration=0.55)
            time.sleep(0.8)
            driver.move_smooth(flag_panel, ice_punch, duration=0.55)
            driver.click(*ice_punch)
            time.sleep(0.9)
            driver.move_smooth(ice_punch, description_panel, duration=0.6)
            time.sleep(0.8)
        elif tool_key == "randomizer":
            for y in (42, 102, 202, 262):
                driver.click(*window_point(window_id, 40, y))
                time.sleep(0.5)
            driver.move(*window_point(window_id, 200, 472))
        elif tool_key == "iso":
            driver.wheel(*window_point(window_id, 260, 310), "down", 8)
            time.sleep(0.8)
            driver.wheel(*window_point(window_id, 260, 310), "up", 5)
            time.sleep(0.8)
            driver.click(*window_point(window_id, 190, 120))

    record_video(output_path, window_id, action, pre_roll_seconds=2.5 if tool_key == "stats" else 1.5)


def capture_screenshots(args: argparse.Namespace, driver: X11Driver, app_path: Path, iso_path: Path, output_dir: Path) -> None:
    screenshots_dir = output_dir / "screenshots"
    scratch_dir = output_dir / ".scratch"

    for key, tool_key in SCREENSHOTS:
        with RunningApp(app_path, iso_path, output_dir / "logs" / f"screenshot-{key}.log"):
            main_window = wait_for_window("Colosseum Tool - Cipher Snagem Editor", timeout=25)
            time.sleep(args.settle_seconds)

            window = main_window
            if tool_key is not None:
                window = open_tool(driver, main_window, tool_key)
                prepare_tool(driver, tool_key, window)

            driver.raise_window(window)
            capture_window_png(window, screenshots_dir / f"cipher-snagem-{key}-desktop.png", scratch_dir)


def capture_videos(args: argparse.Namespace, driver: X11Driver, app_path: Path, iso_path: Path, output_dir: Path) -> None:
    videos_dir = output_dir / "videos"
    videos_dir.mkdir(parents=True, exist_ok=True)
    for stale_video in videos_dir.glob("cipher-snagem-*-desktop.mp4"):
        stale_video.unlink()

    for key, tool_key, trainer_target in VIDEOS:
        with RunningApp(app_path, iso_path, output_dir / "logs" / f"video-{key}.log"):
            main_window = wait_for_window("Colosseum Tool - Cipher Snagem Editor", timeout=25)
            time.sleep(args.settle_seconds)
            tool_window = main_window if tool_key is None else open_tool(driver, main_window, tool_key)
            if trainer_target is not None:
                select_trainer_showcase_target(driver, tool_window, trainer_target, settle_seconds=4.0)
            elif tool_key is not None and tool_key != "trainer":
                prepare_tool(driver, tool_key, tool_window)
            record_tool_flow(
                driver,
                tool_key,
                tool_window,
                videos_dir / f"cipher-snagem-{key}-desktop.mp4",
                trainer_target,
            )


def write_manifest(output_dir: Path, iso_path: Path, app_path: Path) -> None:
    manifest = {
        "product": "cipher-snagem-editor",
        "frame": {"mode": "native-window", "fps": FRAME_RATE},
        "source": {
            "iso": str(iso_path),
            "app": str(app_path),
        },
        "screenshots": [
            {"id": key, "path": f"screenshots/cipher-snagem-{key}-desktop.png"} for key, _ in SCREENSHOTS
        ],
        "videos": [{"id": key, "path": f"videos/cipher-snagem-{key}-desktop.mp4"} for key, _, _ in VIDEOS],
    }
    (output_dir / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--iso", help="Path to a local Pokemon Colosseum ISO.")
    parser.add_argument("--app", help="Path to CipherSnagemEditor.App from a packaged Linux build.")
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT_DIR), help="Output directory for generated media.")
    parser.add_argument("--screenshots-only", action="store_true", help="Capture PNG screenshots only.")
    parser.add_argument("--videos-only", action="store_true", help="Capture MP4 videos only.")
    parser.add_argument("--settle-seconds", type=float, default=2.5, help="Seconds to wait after opening each window.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if args.screenshots_only and args.videos_only:
        raise SystemExit("Choose only one of --screenshots-only or --videos-only.")

    require_command("xwininfo")
    require_command("xwd")
    require_command("gst-launch-1.0")

    iso_path = resolve_first(args.iso, "CIPHER_DEMO_ISO", DEFAULT_ISO_CANDIDATES, "Pokemon Colosseum ISO")
    app_path = resolve_first(args.app, "CIPHER_SNAGEM_EDITOR_APP", DEFAULT_APP_CANDIDATES, "CipherSnagemEditor.App")
    output_dir = Path(args.output).expanduser().resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    driver = X11Driver()
    try:
        if not args.videos_only:
            capture_screenshots(args, driver, app_path, iso_path, output_dir)
        if not args.screenshots_only:
            capture_videos(args, driver, app_path, iso_path, output_dir)
        write_manifest(output_dir, iso_path, app_path)
    finally:
        driver.close()

    print(f"Captured Cipher Snagem Editor demo media in {output_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
