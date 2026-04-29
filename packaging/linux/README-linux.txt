Cipher Snagem Editor Linux Build
================================

This package is built for the runtime named in the archive, for example
linux-x64 for a normal 64-bit Ubuntu PC.

Easiest Ubuntu install:

  Use the .deb package next to this archive:

    cipher-snagem-editor-linux-x64.deb

  In GNOME Files, double-click the .deb and install it. From a terminal:

    sudo apt install ./cipher-snagem-editor-linux-x64.deb

  Then launch "Cipher Snagem Editor" from the app grid.

Portable fallback:

  chmod +x CipherSnagemEditor.App run-cipher-snagem-editor.sh install-linux-user.sh
  ./run-cipher-snagem-editor.sh

Open an ISO from inside the app, or pass one on the command line:

  ./run-cipher-snagem-editor.sh "/path/to/Pokemon Colosseum.iso"

Optional per-user install:

  ./install-linux-user.sh

That copies the app to ~/.local/share/cipher-snagem-editor, installs the PNG
icon under ~/.local/share/icons, and creates a desktop entry under
~/.local/share/applications.

The published build includes the runtime image assets used by the editor panes.
If trainer, Pokemon, move, or type images are missing, rebuild with the current
publish script and make sure the archive contains assets/images.

No game files are included in this package.
