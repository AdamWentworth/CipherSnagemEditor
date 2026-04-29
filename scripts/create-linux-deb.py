#!/usr/bin/env python3
"""Create a simple Debian package from a published Linux app folder."""

from __future__ import annotations

import argparse
import gzip
import io
import os
from pathlib import Path
import tarfile
import time


PACKAGE_NAME = "cipher-snagem-editor"
INSTALL_ROOT = "./opt/cipher-snagem-editor"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--package-dir", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--runtime", default="linux-x64")
    parser.add_argument("--version", default="0.1.0")
    args = parser.parse_args()

    package_dir = args.package_dir.resolve()
    if not package_dir.is_dir():
        raise SystemExit(f"Package directory not found: {package_dir}")

    architecture = architecture_for_runtime(args.runtime)
    data_tar = build_data_tar(package_dir)
    control_tar = build_control_tar(args.version, architecture, installed_size_kb(data_tar))

    args.output.parent.mkdir(parents=True, exist_ok=True)
    write_ar_archive(
        args.output,
        [
            ("debian-binary", b"2.0\n", 0o100644),
            ("control.tar.gz", control_tar, 0o100644),
            ("data.tar.gz", data_tar, 0o100644),
        ],
    )


def architecture_for_runtime(runtime: str) -> str:
    return {
        "linux-x64": "amd64",
        "linux-arm64": "arm64",
    }.get(runtime, runtime)


def installed_size_kb(data_tar_gz: bytes) -> int:
    return max(1, (len(data_tar_gz) + 1023) // 1024)


def build_control_tar(version: str, architecture: str, installed_size: int) -> bytes:
    control = f"""Package: {PACKAGE_NAME}
Version: {version}
Section: games
Priority: optional
Architecture: {architecture}
Maintainer: Cipher Snagem Editor contributors <noreply@example.invalid>
Installed-Size: {installed_size}
Description: Pokemon Colosseum and XD modding editor
 A desktop editor inspired by the original GoD Tool and Pokemon-XD-Code
 Colosseum Tool workflow.
"""

    postinst = """#!/bin/sh
set -e
chmod +x /opt/cipher-snagem-editor/CipherSnagemEditor.App 2>/dev/null || true
chmod +x /opt/cipher-snagem-editor/run-cipher-snagem-editor.sh 2>/dev/null || true
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database /usr/share/applications >/dev/null 2>&1 || true
fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
    gtk-update-icon-cache -q /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi
exit 0
"""

    postrm = """#!/bin/sh
set -e
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database /usr/share/applications >/dev/null 2>&1 || true
fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
    gtk-update-icon-cache -q /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi
exit 0
"""

    entries = [
        ("./control", control.encode("utf-8"), 0o100644),
        ("./postinst", postinst.encode("utf-8"), 0o100755),
        ("./postrm", postrm.encode("utf-8"), 0o100755),
    ]
    return tar_gz_from_entries(entries)


def build_data_tar(package_dir: Path) -> bytes:
    entries: list[tuple[str, bytes | Path | None, int]] = []
    add_dir(entries, "./opt")
    add_dir(entries, INSTALL_ROOT)

    for source in sorted(package_dir.rglob("*")):
        relative = source.relative_to(package_dir)
        if should_skip_portable_file(relative):
            continue

        target = f"{INSTALL_ROOT}/{relative.as_posix()}"
        if source.is_dir():
            add_dir(entries, target)
        elif source.is_file():
            entries.append((target, source, mode_for_payload_file(relative)))

    desktop = """[Desktop Entry]
Version=1.0
Type=Application
Name=Cipher Snagem Editor
Comment=Pokemon Colosseum and Pokemon XD modding editor
Exec=/opt/cipher-snagem-editor/run-cipher-snagem-editor.sh %f
Icon=cipher-snagem-editor
Terminal=false
Categories=Game;Utility;Development;
StartupNotify=true
StartupWMClass=CipherSnagemEditor.App
"""

    icon = package_dir / "resources" / "cipher-snagem-editor.png"
    readme = package_dir / "README-linux.txt"

    add_dir(entries, "./usr")
    add_dir(entries, "./usr/share")
    add_dir(entries, "./usr/share/applications")
    entries.append(("./usr/share/applications/cipher-snagem-editor.desktop", desktop.encode("utf-8"), 0o100644))

    add_dir(entries, "./usr/share/icons")
    add_dir(entries, "./usr/share/icons/hicolor")
    add_dir(entries, "./usr/share/icons/hicolor/256x256")
    add_dir(entries, "./usr/share/icons/hicolor/256x256/apps")
    if icon.is_file():
        entries.append(("./usr/share/icons/hicolor/256x256/apps/cipher-snagem-editor.png", icon, 0o100644))

    add_dir(entries, "./usr/share/doc")
    add_dir(entries, "./usr/share/doc/cipher-snagem-editor")
    if readme.is_file():
        entries.append(("./usr/share/doc/cipher-snagem-editor/README-linux.txt", readme, 0o100644))

    return tar_gz_from_entries(entries)


def should_skip_portable_file(relative: Path) -> bool:
    parts = relative.parts
    if parts and parts[0] == "resources":
        return True

    return relative.name in {
        "cipher-snagem-editor.desktop",
        "install-linux-user.sh",
    }


def mode_for_payload_file(relative: Path) -> int:
    if relative.name in {
        "CipherSnagemEditor.App",
        "createdump",
        "run-cipher-snagem-editor.sh",
    }:
        return 0o100755

    if relative.suffix == ".sh":
        return 0o100755

    return 0o100644


def add_dir(entries: list[tuple[str, bytes | Path | None, int]], name: str) -> None:
    entries.append((name.rstrip("/") + "/", None, 0o040755))


def tar_gz_from_entries(entries: list[tuple[str, bytes | Path | None, int]]) -> bytes:
    buffer = io.BytesIO()
    with gzip.GzipFile(fileobj=buffer, mode="wb", mtime=0) as gzip_file:
        with tarfile.open(fileobj=gzip_file, mode="w") as tar:
            seen: set[str] = set()
            for name, payload, mode in entries:
                if name in seen:
                    continue

                seen.add(name)
                info = tarfile.TarInfo(name)
                info.uid = 0
                info.gid = 0
                info.uname = "root"
                info.gname = "root"
                info.mtime = int(time.time())
                info.mode = mode & 0o7777

                if payload is None:
                    info.type = tarfile.DIRTYPE
                    info.size = 0
                    tar.addfile(info)
                    continue

                if isinstance(payload, Path):
                    data = payload.read_bytes()
                else:
                    data = payload

                info.size = len(data)
                tar.addfile(info, io.BytesIO(data))

    return buffer.getvalue()


def write_ar_archive(output: Path, members: list[tuple[str, bytes, int]]) -> None:
    with output.open("wb") as archive:
        archive.write(b"!<arch>\n")
        for name, data, mode in members:
            write_ar_member(archive, name, data, mode)


def write_ar_member(archive: io.BufferedWriter, name: str, data: bytes, mode: int) -> None:
    if len(name) > 15:
        raise ValueError(f"ar member name is too long: {name}")

    member_name = name + "/"
    header_text = "".join(
        [
            f"{member_name:<16}",
            f"{int(time.time()):<12}",
            f"{0:<6}",
            f"{0:<6}",
            f"{mode:o}".ljust(8),
            f"{len(data):<10}",
            "`\n",
        ]
    )
    header = header_text.encode("ascii")
    if len(header) != 60:
        raise ValueError(f"invalid ar header length for {name}: {len(header)}")

    archive.write(header)
    archive.write(data)
    if len(data) % 2 == 1:
        archive.write(b"\n")


if __name__ == "__main__":
    main()
