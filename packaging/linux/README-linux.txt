Cipher Snagem Editor Linux Build
================================

This package is built for the runtime named in the archive, for example
linux-x64 for a normal 64-bit Ubuntu PC.

Quick test:

  chmod +x CipherSnagemEditor.App run-cipher-snagem-editor.sh install-linux-user.sh
  ./run-cipher-snagem-editor.sh

Open an ISO from inside the app, or pass one on the command line:

  ./run-cipher-snagem-editor.sh "/path/to/Pokemon Colosseum.iso"

Optional per-user install:

  ./install-linux-user.sh

That copies the app to ~/.local/share/cipher-snagem-editor, installs the PNG
icon under ~/.local/share/icons, and creates a desktop entry under
~/.local/share/applications.

No game files are included in this package.
