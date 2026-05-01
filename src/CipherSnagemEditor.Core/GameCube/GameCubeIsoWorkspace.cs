using System.Text.Json;
using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.Files;
using CipherSnagemEditor.Core.Text;

namespace CipherSnagemEditor.Core.GameCube;

public sealed class GameCubeIsoWorkspace
{
    private static readonly JsonSerializerOptions GameStringJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly bool _allowStringGrowth;

    public GameCubeIsoWorkspace(GameCubeIso iso, string workspaceDirectory, bool allowStringGrowth)
    {
        Iso = iso;
        WorkspaceDirectory = workspaceDirectory;
        _allowStringGrowth = allowStringGrowth;
    }

    public GameCubeIso Iso { get; private set; }

    public string WorkspaceDirectory { get; }

    public Dictionary<string, byte[]> LoadedFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, FsysArchive> LoadedFsys { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, GameStringTable> LoadedStringTables { get; } = new(StringComparer.OrdinalIgnoreCase);

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

    public FsysArchive ReadIsoFsysArchive(GameCubeIsoFileEntry entry)
    {
        if (GameFileTypes.FromExtension(entry.Name) != GameFileType.Fsys)
        {
            throw new InvalidDataException($"{entry.Name} is not an FSYS archive.");
        }

        var data = GameCubeIsoReader.ReadFile(Iso, entry);
        var targetPath = ResolveIsoExtractPath(entry.Name, null);
        var archive = FsysArchive.Parse(targetPath, data);
        LoadedFsys[targetPath] = archive;
        return archive;
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

        var validStrings = strings
            .Where(message => message.Id is > 0 and <= 0x000f_ffff)
            .ToArray();
        var table = File.Exists(messagePath)
            ? GameStringTable.Parse(File.ReadAllBytes(messagePath)).WithStrings(validStrings)
            : GameStringTable.FromStrings(validStrings);
        var bytes = table.ToArray(allowGrowth: _allowStringGrowth);
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
            else if (fileType == GameFileType.Script)
            {
                var xdsPath = filePath + ".xds";
                if (File.Exists(xdsPath) && !overwrite)
                {
                    continue;
                }

                if (GameCubeScriptCodec.TryDecompileXds(
                    File.ReadAllBytes(filePath),
                    Path.GetFileName(filePath),
                    out var scriptText,
                    out _))
                {
                    File.WriteAllText(xdsPath, scriptText);
                    yield return xdsPath;
                }
            }
            else if (fileType == GameFileType.Rel
                && Path.GetFileName(filePath).Equals("common.rel", StringComparison.OrdinalIgnoreCase))
            {
                var xdsPath = filePath + ".xds";
                if (File.Exists(xdsPath) && !overwrite)
                {
                    continue;
                }

                var relBytes = File.ReadAllBytes(filePath);
                if (GameCubeScriptCodec.TryFindEmbeddedScript(relBytes, out var scriptOffset, out var scriptLength)
                    && GameCubeScriptCodec.TryDecompileXds(
                        relBytes[scriptOffset..(scriptOffset + scriptLength)],
                        Path.GetFileName(filePath),
                        out var scriptText,
                        out _))
                {
                    File.WriteAllText(xdsPath, scriptText);
                    yield return xdsPath;
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

        foreach (var xdsPath in Directory.EnumerateFiles(folder, "*.xds", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            if (!GameCubeScriptCodec.TryCompileXds(File.ReadAllText(xdsPath), out var scriptBytes, out _))
            {
                continue;
            }

            if (Path.GetFileName(xdsPath).EndsWith(".rel.xds", StringComparison.OrdinalIgnoreCase))
            {
                var relPath = xdsPath[..^".xds".Length];
                WriteEmbeddedRelScript(relPath, scriptBytes);
            }
            else
            {
                var scriptPath = ScriptPathForXdsPath(xdsPath);
                File.WriteAllBytes(scriptPath, scriptBytes);
            }

            yield return xdsPath;
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

    public IsoWriteResult WriteIsoEntry(GameCubeIsoFileEntry entry, byte[] sourceBytes)
    {
        var maximumBytes = MaximumReplacementSize(entry);
        if (entry.TocEntryOffset is null && sourceBytes.Length > entry.Size)
        {
            throw new InvalidDataException($"{entry.Name} cannot grow because it does not have a normal FST size entry.");
        }

        var insertedBytes = 0;
        using (var stream = File.Open(Iso.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
        {
            insertedBytes = EnsureIsoCapacity(stream, entry, sourceBytes.Length, maximumBytes);
            maximumBytes = checked(maximumBytes + (uint)insertedBytes);

            stream.Position = entry.Offset;
            stream.Write(sourceBytes);

            if (entry.Size > sourceBytes.Length)
            {
                WriteZeros(stream, checked((int)(entry.Size - sourceBytes.Length)));
            }

            if (entry.TocEntryOffset is not null)
            {
                Span<byte> sizeBytes = stackalloc byte[4];
                BigEndian.WriteUInt32(sizeBytes, 0, checked((uint)sourceBytes.Length));
                stream.Position = entry.TocEntryOffset.Value + 8;
                stream.Write(sizeBytes);
            }
        }

        LoadedFiles[entry.Name] = sourceBytes;
        Iso = GameCubeIsoReader.Open(Iso.Path);
        return new IsoWriteResult(maximumBytes, insertedBytes);
    }

    private uint MaximumReplacementSize(GameCubeIsoFileEntry entry)
    {
        if (entry.TocEntryOffset is null)
        {
            return entry.Size;
        }

        var nextFile = Iso.Files
            .Where(file => file.Offset > entry.Offset)
            .OrderBy(file => file.Offset)
            .FirstOrDefault();
        var maxEnd = nextFile?.Offset ?? checked((uint)new FileInfo(Iso.Path).Length);
        return maxEnd <= entry.Offset ? entry.Size : maxEnd - entry.Offset;
    }

    private int EnsureIsoCapacity(FileStream stream, GameCubeIsoFileEntry entry, int sourceLength, uint currentMaximumBytes)
    {
        if (sourceLength <= currentMaximumBytes)
        {
            return 0;
        }

        if (entry.TocEntryOffset is null)
        {
            throw new InvalidDataException($"{entry.Name} cannot grow because it does not have a normal FST size entry.");
        }

        var insertedBytes = Align16(checked(sourceLength - (int)currentMaximumBytes));
        var insertOffset = checked((long)entry.Offset + entry.Size);
        InsertZeros(stream, insertOffset, insertedBytes);

        Span<byte> offsetBytes = stackalloc byte[4];
        foreach (var shiftedEntry in Iso.Files
            .Where(file => file.TocEntryOffset is not null && file.Offset > entry.Offset)
            .OrderBy(file => file.Offset))
        {
            BigEndian.WriteUInt32(offsetBytes, 0, checked(shiftedEntry.Offset + (uint)insertedBytes));
            stream.Position = shiftedEntry.TocEntryOffset!.Value + 4;
            stream.Write(offsetBytes);
        }

        UpdateUserDataSize(stream);
        return insertedBytes;
    }

    private static void InsertZeros(FileStream stream, long offset, int count)
    {
        if (count <= 0)
        {
            return;
        }

        var oldLength = stream.Length;
        if (offset < 0 || offset > oldLength)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), $"Offset 0x{offset:x} is outside the ISO.");
        }

        var buffer = new byte[1024 * 1024];
        stream.SetLength(oldLength + count);
        var readEnd = oldLength;
        while (readEnd > offset)
        {
            var readStart = Math.Max(offset, readEnd - buffer.Length);
            var length = checked((int)(readEnd - readStart));
            stream.Position = readStart;
            ReadExactly(stream, buffer.AsSpan(0, length));
            stream.Position = readStart + count;
            stream.Write(buffer, 0, length);
            readEnd = readStart;
        }

        stream.Position = offset;
        WriteZeros(stream, count);
    }

    private static void UpdateUserDataSize(FileStream stream)
    {
        const int userDataStartOffsetLocation = 0x434;
        const int userDataSizeLocation = 0x438;

        Span<byte> headerBytes = stackalloc byte[4];
        stream.Position = userDataStartOffsetLocation;
        ReadExactly(stream, headerBytes);
        var userDataStart = BigEndian.ReadUInt32(headerBytes, 0);
        if (userDataStart == 0 || userDataStart > stream.Length)
        {
            return;
        }

        BigEndian.WriteUInt32(headerBytes, 0, checked((uint)(stream.Length - userDataStart)));
        stream.Position = userDataSizeLocation;
        stream.Write(headerBytes);
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var count = stream.Read(buffer[read..]);
            if (count == 0)
            {
                throw new EndOfStreamException("Unexpected end of ISO while shifting file data.");
            }

            read += count;
        }
    }

    private static int Align16(int value)
        => (value + 0x0f) & ~0x0f;

    private static void WriteZeros(Stream stream, int count)
    {
        Span<byte> zeros = stackalloc byte[4096];
        while (count > 0)
        {
            var length = Math.Min(count, zeros.Length);
            stream.Write(zeros[..length]);
            count -= length;
        }
    }

    public string ResolveIsoExtractPath(string fileName, string? outputPath)
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

    private static string ScriptPathForXdsPath(string xdsPath)
    {
        var directory = Path.GetDirectoryName(xdsPath) ?? string.Empty;
        var fileName = Path.GetFileName(xdsPath);
        if (fileName.EndsWith(".scd.xds", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(directory, fileName[..^".xds".Length]);
        }

        return Path.Combine(directory, Path.GetFileNameWithoutExtension(fileName) + GameFileTypes.ExtensionFor(GameFileType.Script));
    }

    private static void WriteEmbeddedRelScript(string relPath, byte[] scriptBytes)
    {
        if (!File.Exists(relPath))
        {
            throw new FileNotFoundException($"Could not compile embedded script because {Path.GetFileName(relPath)} was not found.", relPath);
        }

        var relBytes = File.ReadAllBytes(relPath);
        if (!GameCubeScriptCodec.TryFindEmbeddedScript(relBytes, out var offset, out var length))
        {
            throw new InvalidDataException($"{Path.GetFileName(relPath)} does not contain a TCOD script block.");
        }

        if (scriptBytes.Length > length)
        {
            throw new InvalidDataException(
                $"{Path.GetFileName(relPath)} embedded script cannot grow from {length:N0} to {scriptBytes.Length:N0} bytes in this raw compiler path.");
        }

        scriptBytes.CopyTo(relBytes.AsSpan(offset));
        if (scriptBytes.Length < length)
        {
            relBytes.AsSpan(offset + scriptBytes.Length, length - scriptBytes.Length).Clear();
        }

        File.WriteAllBytes(relPath, relBytes);
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

    private static string SafeFileName(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = fileName;
        }

        var invalid = Path.GetInvalidFileNameChars();
        var chars = safeFileName.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        return new string(chars);
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
            catch (ArgumentException)
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
