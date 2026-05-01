using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.GameCube;

namespace CipherSnagemEditor.XD;

public sealed partial class XdProjectContext
{
    public string ExtractIsoFile(GameCubeIsoFileEntry entry, string? outputPath = null, bool overwrite = true)
        => IsoWorkspace.ExtractIsoFile(entry, outputPath, overwrite);

    public IsoExportResult ExportIsoFile(
        GameCubeIsoFileEntry entry,
        bool extractFsysContents = true,
        bool decode = true,
        bool overwrite = false)
        => IsoWorkspace.ExportIsoFile(entry, extractFsysContents, decode, overwrite);

    public IsoEncodeResult EncodeIsoFile(GameCubeIsoFileEntry entry)
        => IsoWorkspace.EncodeIsoFile(entry);

    public IsoImportResult ImportIsoFile(GameCubeIsoFileEntry entry, bool encode)
    {
        var result = IsoWorkspace.ImportIsoFile(entry, encode);
        Iso = IsoWorkspace.Iso;
        return result;
    }

    public IsoDeleteResult DeleteIsoFile(GameCubeIsoFileEntry entry)
    {
        var result = IsoWorkspace.DeleteIsoFile(entry);
        Iso = IsoWorkspace.Iso;
        return result;
    }

    public IsoFsysAddFileResult AddFileToIsoFsys(GameCubeIsoFileEntry entry, string sourcePath, ushort shortIdentifier)
    {
        var result = IsoWorkspace.AddFileToIsoFsys(entry, sourcePath, shortIdentifier);
        Iso = IsoWorkspace.Iso;
        return result;
    }

    public FsysArchive ReadIsoFsysArchive(GameCubeIsoFileEntry entry)
        => IsoWorkspace.ReadIsoFsysArchive(entry);

    public string GetIsoExportDirectory(string fileName)
        => IsoWorkspace.GetIsoExportDirectory(fileName);
}
