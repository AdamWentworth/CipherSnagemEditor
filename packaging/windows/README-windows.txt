Cipher Snagem Editor Windows Build
===================================

This package is a self-contained Windows x64 build. You do not need to install
the .NET runtime separately.

Quick start:

  1. Extract the entire zip folder.
  2. Run the executable in this folder:

       ColosseumTool.exe

     or:

       GoDTool.exe

     The package you downloaded contains one of these tools.

Optional Start Menu install:

  From PowerShell inside the extracted folder:

    powershell -ExecutionPolicy Bypass -File .\install-windows-user.ps1

  That copies the app to:

    %LOCALAPPDATA%\CipherSnagemEditor

  and creates a Start Menu shortcut under:

    Cipher Snagem Editor

  Add a desktop shortcut too:

    powershell -ExecutionPolicy Bypass -File .\install-windows-user.ps1 -DesktopShortcut

Uninstall optional shortcut install:

    powershell -ExecutionPolicy Bypass -File .\uninstall-windows-user.ps1

No game files are included in this package. Open your own clean Pokemon
Colosseum or Pokemon XD ISO from inside the app.
