#!/usr/bin/env python3
"""Create a simple Debian package from a published Linux app folder."""

from __future__ import annotations

import argparse
import gzip
import io
from pathlib import Path
import tarfile
import time


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--package-dir", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--runtime", default="linux-x64")
    parser.add_argument("--version", default="0.1.0")
    parser.add_argument("--package-name", default="cipher-snagem-editor")
    parser.add_argument("--install-root", default="/opt/cipher-snagem-editor")
    parser.add_argument("--app-name", default="Cipher Snagem Editor")
    parser.add_argument("--comment", default="Pokemon Colosseum and Pokemon XD modding editor")
    parser.add_argument("--icon-name", default="cipher-snagem-editor")
    parser.add_argument("--executable", default="CipherSnagemEditor.App")
    args = parser.parse_args()

    package_dir = args.package_dir.resolve()
    if not package_dir.is_dir():
        raise SystemExit(f"Package directory not found: {package_dir}")

    architecture = architecture_for_runtime(args.runtime)
    install_root = "." + args.install_root.rstrip("/")
    data_tar = build_data_tar(
        package_dir,
        install_root,
        args.app_name,
        args.comment,
        args.icon_name,
        args.executable,
        f"{args.package_name}.desktop",
    )
    control_tar = build_control_tar(
        args.version,
        architecture,
        installed_size_kb(data_tar),
        args.package_name,
        args.comment,
        args.executable,
        args.install_root.rstrip("/"),
    )

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


def build_control_tar(
    version: str,
    architecture: str,
    installed_size: int,
    package_name: str,
    description: str,
    executable: str,
    install_root: str,
) -> bytes:
    control = f"""Package: {package_name}
Version: {version}
Section: games
Priority: optional
Architecture: {architecture}
Maintainer: Cipher Snagem Editor contributors <noreply@example.invalid>
Installed-Size: {installed_size}
Description: {description}
 A desktop editor inspired by StarsMMD's original GoD Tool and Pokemon-XD-Code
 workflows.
"""

    postinst = f"""#!/bin/sh
set -e
chmod +x {install_root}/{executable} 2>/dev/null || true
chmod +x {install_root}/run-cipher-snagem-editor.sh 2>/dev/null || true
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


def build_data_tar(
    package_dir: Path,
    install_root: str,
    app_name: str,
    comment: str,
    icon_name: str,
    executable: str,
    desktop_file_name: str,
) -> bytes:
    entries: list[tuple[str, bytes | Path | None, int]] = []
    add_dir(entries, "./opt")
    add_dir(entries, install_root)

    for source in sorted(package_dir.rglob("*")):
        relative = source.relative_to(package_dir)
        if should_skip_portable_file(relative):
            continue

        target = f"{install_root}/{relative.as_posix()}"
        if source.is_dir():
            add_dir(entries, target)
        elif source.is_file():
            entries.append((target, source, mode_for_payload_file(relative, executable)))

    desktop = """[Desktop Entry]
Version=1.0
Type=Application
Name={app_name}
Comment={comment}
Exec={install_root_without_dot}/run-cipher-snagem-editor.sh %f
Icon={icon_name}
Terminal=false
Categories=Game;Utility;Development;
StartupNotify=true
StartupWMClass={executable}
""".format(
        app_name=app_name,
        comment=comment,
        install_root_without_dot=install_root.removeprefix("."),
        icon_name=icon_name,
        executable=executable,
    )

    icon = package_dir / "resources" / f"{icon_name}.png"
    readme = package_dir / "README-linux.txt"

    add_dir(entries, "./usr")
    add_dir(entries, "./usr/share")
    add_dir(entries, "./usr/share/applications")
    entries.append((f"./usr/share/applications/{desktop_file_name}", desktop.encode("utf-8"), 0o100644))

    add_dir(entries, "./usr/share/icons")
    add_dir(entries, "./usr/share/icons/hicolor")
    add_dir(entries, "./usr/share/icons/hicolor/256x256")
    add_dir(entries, "./usr/share/icons/hicolor/256x256/apps")
    if icon.is_file():
        entries.append((f"./usr/share/icons/hicolor/256x256/apps/{icon_name}.png", icon, 0o100644))

    add_dir(entries, "./usr/share/doc")
    add_dir(entries, f"./usr/share/doc/{desktop_file_name.removesuffix('.desktop')}")
    if readme.is_file():
        entries.append((f"./usr/share/doc/{desktop_file_name.removesuffix('.desktop')}/README-linux.txt", readme, 0o100644))

    return tar_gz_from_entries(entries)


def should_skip_portable_file(relative: Path) -> bool:
    parts = relative.parts
    if parts and parts[0] == "resources":
        return True

    return relative.name in {
        "cipher-snagem-editor.desktop",
        "install-linux-user.sh",
    }


def mode_for_payload_file(relative: Path, executable: str) -> int:
    if relative.name in {
        executable,
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
