using System.Text.Json;
using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.Files;
using CipherSnagemEditor.Core.GameCube;
using CipherSnagemEditor.Core.Text;

namespace CipherSnagemEditor.XD;

public sealed partial class XdProjectContext
{
    private static readonly JsonSerializerOptions GameStringJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public string ExtractIsoFile(GameCubeIsoFileEntry entry, string? outputPath = null, bool overwrite = true)
    {
        var targetPath = ResolveIsoExtractPath(entry.Name, outputPath);
        if (!overwrite && File.Exists(targetPath))
        {
            throw new IOException($"Refusing to overwrite existing file: {targetPath}");
        }

        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        var data = GameCubeIsoReader.ReadFile(Iso, entry);
        File.WriteAllBytes(targetPath, data);
        LoadedFiles[entry.Name] = data;
        return targetPath;
    }

    public IsoExportResult ExportIsoFile(
        GameCubeIsoFileEntry entry,
        bool extractFsysContents = true,
        bool decode = true,
        bool overwrite = false)
    {
        var targetPath = ResolveIsoExtractPath(entry.Name, null);
        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        var data = GameCubeIsoReader.ReadFile(Iso, entry);
        if (!File.Exists(targetPath) || overwrite)
        {
            File.WriteAllBytes(targetPath, data);
        }

        LoadedFiles[entry.Name] = data;

        var extractedFiles = new List<string>();
        var decodedFiles = new List<string>();
        var entryFileType = GameFileTypes.FromExtension(entry.Name);
        if (entryFileType == GameFileType.Fsys)
        {
            var folder = GetIsoExportDirectory(entry.Name);
            var archive = FsysArchive.Parse(targetPath, data);
            LoadedFsys[targetPath] = archive;

            if (extractFsysContents)
            {
                extractedFiles.AddRange(ExtractFsysFiles(archive, folder, overwrite));
            }

            if (decode)
            {
                decodedFiles.AddRange(DecodeExtractedFsysFiles(archive, folder, overwrite));
                decodedFiles.AddRange(DecodeWorkspaceBinaryFiles(folder, overwrite));
            }
        }
        else if (decode && entryFileType is GameFileType.Gtx or GameFileType.Atx)
        {
            var pngPath = targetPath + ".png";
            if ((!File.Exists(pngPath) || overwrite)
                && GameCubeTextureCodec.TryDecodePng(File.ReadAllBytes(targetPath), out var pngBytes))
            {
                File.WriteAllBytes(pngPath, pngBytes);
                decodedFiles.Add(pngPath);
            }
        }
        else if (decode && entryFileType == GameFileType.Gsw)
        {
            decodedFiles.AddRange(DecodeGswTextures(targetPath, overwrite));
        }

        return new IsoExportResult(targetPath, extractedFiles, decodedFiles);
    }

    public IsoEncodeResult EncodeIsoFile(GameCubeIsoFileEntry entry)
        => PrepareWorkspaceIsoFile(entry, encodeDecodedFiles: true, packArchive: true);

    public IsoImportResult ImportIsoFile(GameCubeIsoFileEntry entry, bool encode)
    {
        if (string.Equals(entry.Name, "Game.toc", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("Replacing Game.toc directly is not supported.");
        }

        var encodeResult = PrepareWorkspaceIsoFile(
            entry,
            encodeDecodedFiles: encode,
            packArchive: GameFileTypes.FromExtension(entry.Name) == GameFileType.Fsys);
        var sourceBytes = File.ReadAllBytes(encodeResult.FilePath);
        var writeResult = WriteIsoEntry(entry, sourceBytes);
        return new IsoImportResult(
            encodeResult.FilePath,
            sourceBytes.Length,
            writeResult.MaximumBytes,
            writeResult.InsertedBytes,
            encodeResult);
    }

    public IsoDeleteResult DeleteIsoFile(GameCubeIsoFileEntry entry)
    {
        if (string.Equals(entry.Name, "Start.dol", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.Name, "Game.toc", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"{entry.Name} cannot be deleted.");
        }

        var backupPath = ResolveIsoExtractPath(entry.Name, null);
        if (!File.Exists(backupPath))
        {
            backupPath = ExportIsoFile(entry, extractFsysContents: true, decode: false, overwrite: false).FilePath;
        }

        var replacement = GameFileTypes.FromExtension(entry.Name) == GameFileType.Fsys
            ? NullFsys()
            : "DELETED DELETED\0"u8.ToArray();
        if (replacement.Length > entry.Size)
        {
            throw new InvalidDataException($"{entry.Name} is too small to replace with the legacy deleted marker.");
        }

        WriteIsoEntry(entry, replacement);
        return new IsoDeleteResult(entry.Name, replacement.Length, backupPath);
    }

    public IsoFsysAddFileResult AddFileToIsoFsys(GameCubeIsoFileEntry entry, string sourcePath, ushort shortIdentifier)
    {
        if (GameFileTypes.FromExtension(entry.Name) != GameFileType.Fsys)
        {
            throw new InvalidDataException($"{entry.Name} is not an FSYS archive.");
        }

        var targetPath = EnsureRawIsoWorkspaceFile(entry);
        var folder = GetIsoExportDirectory(entry.Name);
        Directory.CreateDirectory(folder);

        var archive = FsysArchive.Load(targetPath);
        var addResult = archive.AddFile(sourcePath, shortIdentifier, compress: true);
        File.WriteAllBytes(targetPath, addResult.ArchiveBytes);
        LoadedFsys[targetPath] = FsysArchive.Parse(targetPath, addResult.ArchiveBytes);
        LoadedFiles[entry.Name] = addResult.ArchiveBytes;

        var workspaceFilePath = Path.Combine(folder, addResult.EntryName);
        if (!string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(workspaceFilePath), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(sourcePath, workspaceFilePath, overwrite: true);
        }

        var importResult = ImportIsoFile(entry, encode: false);
        return new IsoFsysAddFileResult(
            targetPath,
            workspaceFilePath,
            addResult.EntryName,
            addResult.ShortIdentifier,
            addResult.SourceSize,
            addResult.ArchiveSize,
            addResult.Compressed,
            importResult);
    }

    private IsoEncodeResult PrepareWorkspaceIsoFile(
        GameCubeIsoFileEntry entry,
        bool encodeDecodedFiles,
        bool packArchive)
    {
        var targetPath = EnsureRawIsoWorkspaceFile(entry);
        var encodedFiles = new List<string>();
        var packedFiles = new List<string>();

        switch (GameFileTypes.FromExtension(entry.Name))
        {
            case GameFileType.Fsys:
            {
                var folder = GetIsoExportDirectory(entry.Name);
                Directory.CreateDirectory(folder);
                if (encodeDecodedFiles)
                {
                    encodedFiles.AddRange(EncodeDecodedMessageFiles(folder));
                    encodedFiles.AddRange(EncodeWorkspaceBinaryFiles(folder));
                }

                if (packArchive)
                {
                    var archive = FsysArchive.Load(targetPath);
                    var result = archive.ReplaceFilesFromDirectory(folder, encodeCompressed: true);
                    File.WriteAllBytes(targetPath, result.ArchiveBytes);
                    LoadedFsys[targetPath] = FsysArchive.Parse(targetPath, result.ArchiveBytes);
                    packedFiles.AddRange(result.ReplacedFiles.Select(file => file.SourcePath));
                }

                break;
            }

            case GameFileType.Message:
            {
                if (encodeDecodedFiles)
                {
                    var jsonPath = targetPath + ".json";
                    if (File.Exists(jsonPath))
                    {
                        EncodeMessageJson(jsonPath, targetPath);
                        encodedFiles.Add(jsonPath);
                    }
                }

                break;
            }

            case GameFileType.Gtx:
            case GameFileType.Atx:
            {
                if (encodeDecodedFiles)
                {
                    var pngPath = targetPath + ".png";
                    if (File.Exists(pngPath)
                        && GameCubeTextureCodec.TryImportPng(File.ReadAllBytes(targetPath), File.ReadAllBytes(pngPath), out var importedTexture))
                    {
                        File.WriteAllBytes(targetPath, importedTexture);
                        encodedFiles.Add(pngPath);
                    }
                }

                break;
            }

            case GameFileType.Gsw:
            {
                if (encodeDecodedFiles)
                {
                    encodedFiles.AddRange(EncodeGswTextures(targetPath));
                }

                break;
            }
        }

        return new IsoEncodeResult(targetPath, encodedFiles, packedFiles);
    }

    private string EnsureRawIsoWorkspaceFile(GameCubeIsoFileEntry entry)
    {
        var targetPath = ResolveIsoExtractPath(entry.Name, null);
        if (File.Exists(targetPath))
        {
            return targetPath;
        }

        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        var data = GameCubeIsoReader.ReadFile(Iso, entry);
        File.WriteAllBytes(targetPath, data);
        LoadedFiles[entry.Name] = data;
        return targetPath;
    }

    private IEnumerable<string> EncodeDecodedMessageFiles(string folder)
    {
        if (!Directory.Exists(folder))
        {
            yield break;
        }

        foreach (var jsonPath in Directory.EnumerateFiles(folder, "*.msg.json", SearchOption.TopDirectoryOnly))
        {
            var messagePath = jsonPath[..^".json".Length];
            EncodeMessageJson(jsonPath, messagePath);
            yield return jsonPath;
        }
    }

    private void EncodeMessageJson(string jsonPath, string messagePath)
    {
        var strings = JsonSerializer.Deserialize<GameString[]>(File.ReadAllText(jsonPath), GameStringJsonOptions);
        if (strings is null)
        {
            throw new InvalidDataException($"Could not read message JSON: {jsonPath}");
        }

        var table = File.Exists(messagePath)
            ? GameStringTable.Parse(File.ReadAllBytes(messagePath)).WithStrings(strings)
            : GameStringTable.FromStrings(strings);
        var bytes = table.ToArray(allowGrowth: Settings.IncreaseFileSizes);
        File.WriteAllBytes(messagePath, bytes);
        LoadedStringTables[messagePath] = table;
        LoadedFiles[messagePath] = bytes;
    }

    private static IEnumerable<string> DecodeWorkspaceBinaryFiles(string folder, bool overwrite)
    {
        if (!Directory.Exists(folder))
        {
            yield break;
        }

        foreach (var filePath in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var fileType = GameFileTypes.FromExtension(filePath);
            if (fileType == GameFileType.Pkx)
            {
                var datPath = filePath + ".dat";
                if (File.Exists(datPath) && !overwrite)
                {
                    continue;
                }

                if (GameCubeLegacyFileCodecs.TryExportPkxDat(File.ReadAllBytes(filePath), out var dat))
                {
                    File.WriteAllBytes(datPath, dat);
                    yield return datPath;
                }
            }
            else if (fileType == GameFileType.Wzx)
            {
                foreach (var model in GameCubeLegacyFileCodecs.ExtractWzxDatModels(File.ReadAllBytes(filePath)))
                {
                    var modelPath = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(filePath)}_{model.Index}.wzx.dat");
                    if (File.Exists(modelPath) && !overwrite)
                    {
                        continue;
                    }

                    File.WriteAllBytes(modelPath, model.Data);
                    yield return modelPath;
                }
            }
            else if (fileType == GameFileType.Thh)
            {
                var basePath = Path.Combine(folder, Path.GetFileNameWithoutExtension(filePath));
                var bodyPath = basePath + GameFileTypes.ExtensionFor(GameFileType.Thd);
                if (!File.Exists(bodyPath))
                {
                    continue;
                }

                var thpPath = basePath + GameFileTypes.ExtensionFor(GameFileType.Thp);
                if (File.Exists(thpPath) && !overwrite)
                {
                    continue;
                }

                File.WriteAllBytes(
                    thpPath,
                    GameCubeLegacyFileCodecs.CombineThp(
                        File.ReadAllBytes(filePath),
                        File.ReadAllBytes(bodyPath)));
                yield return thpPath;
            }
            else if (fileType is GameFileType.Gtx or GameFileType.Atx)
            {
                var pngPath = filePath + ".png";
                if (File.Exists(pngPath) && !overwrite)
                {
                    continue;
                }

                if (GameCubeTextureCodec.TryDecodePng(File.ReadAllBytes(filePath), out var pngBytes))
                {
                    File.WriteAllBytes(pngPath, pngBytes);
                    yield return pngPath;
                }
            }
            else if (fileType == GameFileType.Gsw)
            {
                foreach (var decodedFile in DecodeGswTextures(filePath, overwrite))
                {
                    yield return decodedFile;
                }
            }
        }

        foreach (var modelPath in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
            .Where(path => GameFileTypes.FromExtension(path) is GameFileType.Dat or GameFileType.RoomData)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var texture in GameCubeDatTextureCodec.ExtractTextures(File.ReadAllBytes(modelPath)))
            {
                var texturePath = ModelTexturePath(modelPath, texture.Index);
                if (!File.Exists(texturePath) || overwrite)
                {
                    File.WriteAllBytes(texturePath, texture.TextureBytes);
                    yield return texturePath;
                }

                var pngPath = texturePath + ".png";
                if ((!File.Exists(pngPath) || overwrite)
                    && GameCubeTextureCodec.TryDecodePng(File.ReadAllBytes(texturePath), out var pngBytes))
                {
                    File.WriteAllBytes(pngPath, pngBytes);
                    yield return pngPath;
                }
            }
        }
    }

    private static IEnumerable<string> EncodeWorkspaceBinaryFiles(string folder)
    {
        if (!Directory.Exists(folder))
        {
            yield break;
        }

        foreach (var thpPath in Directory.EnumerateFiles(folder, "*.thp", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            if (!GameCubeLegacyFileCodecs.TrySplitThp(File.ReadAllBytes(thpPath), out var header, out var body))
            {
                continue;
            }

            var basePath = Path.Combine(folder, Path.GetFileNameWithoutExtension(thpPath));
            File.WriteAllBytes(basePath + GameFileTypes.ExtensionFor(GameFileType.Thh), header);
            File.WriteAllBytes(basePath + GameFileTypes.ExtensionFor(GameFileType.Thd), body);
            yield return thpPath;
        }

        foreach (var texturePath in Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
            .Where(path => GameFileTypes.FromExtension(path) is GameFileType.Gtx or GameFileType.Atx)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var pngPath = texturePath + ".png";
            if (!File.Exists(pngPath))
            {
                continue;
            }

            if (GameCubeTextureCodec.TryImportPng(
                File.ReadAllBytes(texturePath),
                File.ReadAllBytes(pngPath),
                out var importedTexture))
            {
                File.WriteAllBytes(texturePath, importedTexture);
                yield return pngPath;
            }
        }

        foreach (var gswPath in Directory.EnumerateFiles(folder, "*.gsw", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var importedTexturePath in EncodeGswTextures(gswPath))
            {
                yield return importedTexturePath;
            }
        }

        foreach (var modelPath in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
            .Where(path => GameFileTypes.FromExtension(path) is GameFileType.Dat or GameFileType.RoomData)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var replacements = GameCubeDatTextureCodec.ExtractTextures(File.ReadAllBytes(modelPath))
                .Select(texture => (texture.Index, Path: ModelTexturePath(modelPath, texture.Index)))
                .Where(texture => File.Exists(texture.Path))
                .ToDictionary(texture => texture.Index, texture => File.ReadAllBytes(texture.Path));
            if (replacements.Count == 0)
            {
                continue;
            }

            if (GameCubeDatTextureCodec.TryImportTextures(
                File.ReadAllBytes(modelPath),
                replacements,
                out var importedModel,
                out var importedCount)
                && importedCount > 0)
            {
                File.WriteAllBytes(modelPath, importedModel);
                yield return modelPath;
            }
        }

        foreach (var pkxPath in Directory.EnumerateFiles(folder, "*.pkx", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var datPath = pkxPath + ".dat";
            if (!File.Exists(datPath))
            {
                continue;
            }

            if (GameCubeLegacyFileCodecs.TryImportPkxDat(
                File.ReadAllBytes(pkxPath),
                File.ReadAllBytes(datPath),
                out var importedPkx))
            {
                File.WriteAllBytes(pkxPath, importedPkx);
                yield return datPath;
            }
        }

        foreach (var wzxPath in Directory.EnumerateFiles(folder, "*.wzx", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var wzxBytes = File.ReadAllBytes(wzxPath);
            var models = GameCubeLegacyFileCodecs.ExtractWzxDatModels(wzxBytes);
            var changed = false;
            foreach (var model in models)
            {
                var modelPath = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(wzxPath)}_{model.Index}.wzx.dat");
                if (!File.Exists(modelPath))
                {
                    continue;
                }

                if (!GameCubeLegacyFileCodecs.TryImportWzxDatModel(
                    wzxBytes,
                    model.Index,
                    File.ReadAllBytes(modelPath),
                    out var importedWzx))
                {
                    continue;
                }

                wzxBytes = importedWzx;
                changed = true;
                yield return modelPath;
            }

            if (changed)
            {
                File.WriteAllBytes(wzxPath, wzxBytes);
            }
        }
    }

    private string ResolveIsoExtractPath(string fileName, string? outputPath)
    {
        var safeFileName = SafeFileName(fileName);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Path.Combine(GetIsoExportDirectory(safeFileName), safeFileName);
        }

        if (Directory.Exists(outputPath)
            || outputPath.EndsWith(Path.DirectorySeparatorChar)
            || outputPath.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return Path.Combine(outputPath, safeFileName);
        }

        return outputPath;
    }

    public string GetIsoExportDirectory(string fileName)
    {
        var safeFileName = SafeFileName(fileName);
        var folderName = RemoveFileExtensionsLikeLegacy(safeFileName);
        if (string.IsNullOrWhiteSpace(folderName))
        {
            folderName = safeFileName;
        }

        return Path.Combine(WorkspaceDirectory, "Game Files", folderName);
    }

    private static string ModelTexturePath(string modelPath, int textureIndex)
    {
        var directory = Path.GetDirectoryName(modelPath) ?? string.Empty;
        var fileName = Path.GetFileName(modelPath);
        var extensionIndex = fileName.IndexOf('.');
        var stem = extensionIndex < 0 ? fileName : fileName[..extensionIndex];
        var extensions = extensionIndex < 0 ? string.Empty : fileName[extensionIndex..];
        return Path.Combine(directory, $"{stem}_{textureIndex}{extensions}.gtx");
    }

    private static string GswTexturePath(string gswPath, int textureId)
    {
        var directory = Path.GetDirectoryName(gswPath) ?? string.Empty;
        var stem = RemoveFileExtensionsLikeLegacy(Path.GetFileName(gswPath));
        return Path.Combine(directory, $"{stem}_gsw_{textureId}.gtx");
    }

    private static IEnumerable<string> DecodeGswTextures(string gswPath, bool overwrite)
    {
        foreach (var texture in GameCubeGswTextureCodec.ExtractTextures(File.ReadAllBytes(gswPath)))
        {
            var texturePath = GswTexturePath(gswPath, texture.Id);
            if (!File.Exists(texturePath) || overwrite)
            {
                File.WriteAllBytes(texturePath, texture.TextureBytes);
                yield return texturePath;
            }

            var pngPath = texturePath + ".png";
            if ((!File.Exists(pngPath) || overwrite)
                && GameCubeTextureCodec.TryDecodePng(File.ReadAllBytes(texturePath), out var pngBytes))
            {
                File.WriteAllBytes(pngPath, pngBytes);
                yield return pngPath;
            }
        }
    }

    private static IEnumerable<string> EncodeGswTextures(string gswPath)
    {
        var replacements = GameCubeGswTextureCodec.ExtractTextures(File.ReadAllBytes(gswPath))
            .Select(texture => (texture.Id, Path: GswTexturePath(gswPath, texture.Id)))
            .Where(texture => File.Exists(texture.Path))
            .ToDictionary(texture => texture.Id, texture => File.ReadAllBytes(texture.Path));
        if (replacements.Count == 0)
        {
            yield break;
        }

        if (GameCubeGswTextureCodec.TryImportTextures(
            File.ReadAllBytes(gswPath),
            replacements,
            out var importedGsw,
            out var importedCount)
            && importedCount > 0)
        {
            File.WriteAllBytes(gswPath, importedGsw);
            foreach (var texturePath in replacements.Keys.Select(id => GswTexturePath(gswPath, id)).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                yield return texturePath;
            }
        }
    }

    private static string RemoveFileExtensionsLikeLegacy(string fileName)
    {
        var extensionIndex = fileName.IndexOf('.');
        return extensionIndex < 0 ? fileName : fileName[..extensionIndex];
    }

    private static IEnumerable<string> ExtractFsysFiles(FsysArchive archive, string folder, bool overwrite)
    {
        Directory.CreateDirectory(folder);

        foreach (var entry in archive.Entries)
        {
            var outputPath = Path.Combine(folder, SafeFileName(entry.Name));
            if (File.Exists(outputPath) && !overwrite)
            {
                continue;
            }

            File.WriteAllBytes(outputPath, archive.Extract(entry));
            yield return outputPath;
        }
    }

    private IEnumerable<string> DecodeExtractedFsysFiles(FsysArchive archive, string folder, bool overwrite)
    {
        foreach (var entry in archive.Entries)
        {
            if (entry.FileType != GameFileType.Message)
            {
                continue;
            }

            var messagePath = Path.Combine(folder, SafeFileName(entry.Name));
            if (!File.Exists(messagePath))
            {
                continue;
            }

            var jsonPath = messagePath + ".json";
            if (File.Exists(jsonPath) && !overwrite)
            {
                continue;
            }

            GameStringTable table;
            try
            {
                table = GameStringTable.Load(messagePath);
            }
            catch (InvalidDataException)
            {
                continue;
            }
            catch (EndOfStreamException)
            {
                continue;
            }

            LoadedStringTables[messagePath] = table;
            var json = JsonSerializer.Serialize(table.Strings, GameStringJsonOptions);
            File.WriteAllText(jsonPath, json);
            yield return jsonPath;
        }
    }

    private static byte[] NullFsys()
        =>
        [
            0x46, 0x53, 0x59, 0x53, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x60, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x4E, 0x55, 0x4C, 0x4C, 0x46, 0x53, 0x59, 0x53
        ];
}
