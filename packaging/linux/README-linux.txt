@APP_NAME@ Linux Build
======================

This package is built for the runtime named in the archive, for example
linux-x64 for a normal 64-bit Ubuntu PC.

Easiest Ubuntu install:

  Use the .deb package next to this archive:

    @APP_SLUG@-linux-x64.deb

  In GNOME Files, double-click the .deb and install it. From a terminal:

    sudo apt install ./@APP_SLUG@-linux-x64.deb

  Then launch "@APP_NAME@" from the app grid.

Portable fallback:

  chmod +x @EXECUTABLE@ run-cipher-snagem-editor.sh install-linux-user.sh
  ./run-cipher-snagem-editor.sh

Open an ISO from inside the app, or pass one on the command line:

  ./run-cipher-snagem-editor.sh "/path/to/Pokemon Colosseum.iso"

Optional per-user install:

  ./install-linux-user.sh

That copies the app to ~/.local/share/@APP_SLUG@, installs the PNG icon under
~/.local/share/icons, and creates a desktop entry under
~/.local/share/applications.

The published build includes the runtime image assets used by the editor panes.
If trainer, Pokemon, move, or type images are missing, rebuild with the current
publish script and make sure the archive contains assets/images.

No game files are included in this package.
