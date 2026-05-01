#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DATA_HOME="${XDG_DATA_HOME:-$HOME/.local/share}"
INSTALL_DIR="$DATA_HOME/@APP_SLUG@"
DESKTOP_DIR="$DATA_HOME/applications"
ICON_DIR="$DATA_HOME/icons/hicolor/256x256/apps"

mkdir -p "$DESKTOP_DIR" "$ICON_DIR"
if [ "$SCRIPT_DIR" != "$INSTALL_DIR" ]; then
    rm -rf "$INSTALL_DIR"
    mkdir -p "$INSTALL_DIR"
    cp -a "$SCRIPT_DIR"/. "$INSTALL_DIR"/
else
    echo "Already running from $INSTALL_DIR"
fi

cp "$INSTALL_DIR/resources/@ICON_NAME@.png" "$ICON_DIR/@ICON_NAME@.png"
sed "s|@INSTALL_DIR@|$INSTALL_DIR|g" \
    "$INSTALL_DIR/@APP_SLUG@.desktop" \
    > "$DESKTOP_DIR/@APP_SLUG@.desktop"

chmod +x "$INSTALL_DIR/@EXECUTABLE@"
chmod +x "$INSTALL_DIR/run-cipher-snagem-editor.sh"
chmod +x "$DESKTOP_DIR/@APP_SLUG@.desktop"

if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$DESKTOP_DIR" >/dev/null 2>&1 || true
fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
    gtk-update-icon-cache -q "$DATA_HOME/icons/hicolor" >/dev/null 2>&1 || true
fi

echo "Installed @APP_NAME@ to $INSTALL_DIR"
echo "Desktop entry: $DESKTOP_DIR/@APP_SLUG@.desktop"
