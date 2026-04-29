#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DATA_HOME="${XDG_DATA_HOME:-$HOME/.local/share}"
INSTALL_DIR="$DATA_HOME/cipher-snagem-editor"
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

cp "$INSTALL_DIR/resources/cipher-snagem-editor.png" "$ICON_DIR/cipher-snagem-editor.png"
sed "s|@INSTALL_DIR@|$INSTALL_DIR|g" \
    "$INSTALL_DIR/cipher-snagem-editor.desktop" \
    > "$DESKTOP_DIR/cipher-snagem-editor.desktop"

chmod +x "$INSTALL_DIR/CipherSnagemEditor.App"
chmod +x "$INSTALL_DIR/run-cipher-snagem-editor.sh"
chmod +x "$DESKTOP_DIR/cipher-snagem-editor.desktop"

echo "Installed Cipher Snagem Editor to $INSTALL_DIR"
echo "Desktop entry: $DESKTOP_DIR/cipher-snagem-editor.desktop"
