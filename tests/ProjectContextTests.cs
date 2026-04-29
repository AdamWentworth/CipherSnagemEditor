using CipherSnagemEditor.Colosseum;
using CipherSnagemEditor.Core.Archives;
using CipherSnagemEditor.Core.Binary;
using CipherSnagemEditor.Core.Files;
using CipherSnagemEditor.Core.Text;
using System.Text.Json;

namespace CipherSnagemEditor.Tests;

public sealed class ProjectContextTests
{
    [Fact]
    public void OpensMinimalColosseumIsoAndCreatesLegacyWorkspace()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var bytes = new byte[0x500];
        bytes[0] = (byte)'G';
        bytes[1] = (byte)'C';
        bytes[2] = (byte)'6';
        bytes[3] = (byte)'E';
        BigEndian.WriteUInt32(bytes, 0x420, 0x440);
        BigEndian.WriteUInt32(bytes, 0x424, 0x460);
        BigEndian.WriteUInt32(bytes, 0x428, 0x0c);
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);

        Assert.Equal(ColosseumSourceKind.Iso, context.SourceKind);
        Assert.Equal("GC6E", context.Iso?.GameId);
        Assert.Equal(Path.Combine(temp, "Sample CM Tool"), context.WorkspaceDirectory);
        Assert.True(Directory.Exists(context.WorkspaceDirectory));
        Assert.True(File.Exists(Path.Combine(context.WorkspaceDirectory!, "Settings.json")));
    }

    [Fact]
    public void ExtractsIsoFileToLegacyGameFilesFolder()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var bytes = new byte[0x700];
        bytes[0] = (byte)'G';
        bytes[1] = (byte)'C';
        bytes[2] = (byte)'6';
        bytes[3] = (byte)'E';

        var fileNameBytes = "sample.bin\0"u8.ToArray();
        BigEndian.WriteUInt32(bytes, 0x420, 0x100);
        BigEndian.WriteUInt32(bytes, 0x424, 0x300);
        BigEndian.WriteUInt32(bytes, 0x428, (uint)(0x18 + fileNameBytes.Length));
        BigEndian.WriteUInt32(bytes, 0x300, 0x01000000);
        BigEndian.WriteUInt32(bytes, 0x304, 0);
        BigEndian.WriteUInt32(bytes, 0x308, 2);
        BigEndian.WriteUInt32(bytes, 0x30c, 0);
        BigEndian.WriteUInt32(bytes, 0x310, 0x500);
        BigEndian.WriteUInt32(bytes, 0x314, 3);
        fileNameBytes.CopyTo(bytes, 0x318);
        bytes[0x500] = 1;
        bytes[0x501] = 2;
        bytes[0x502] = 3;
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);
        var entry = Assert.Single(context.Iso!.Files, file => file.Name == "sample.bin");

        var extractedPath = context.ExtractIsoFile(entry);

        Assert.Equal(Path.Combine(temp, "Sample CM Tool", "Game Files", "sample", "sample.bin"), extractedPath);
        Assert.Equal([1, 2, 3], File.ReadAllBytes(extractedPath));
    }

    [Fact]
    public void ExportsFsysIsoFileAndExtractsInnerFiles()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var fsysBytes = CreateSingleEntryFsys();
        var bytes = new byte[0x900];
        bytes[0] = (byte)'G';
        bytes[1] = (byte)'C';
        bytes[2] = (byte)'6';
        bytes[3] = (byte)'E';

        var fileNameBytes = "sample.fsys\0"u8.ToArray();
        BigEndian.WriteUInt32(bytes, 0x420, 0x100);
        BigEndian.WriteUInt32(bytes, 0x424, 0x300);
        BigEndian.WriteUInt32(bytes, 0x428, (uint)(0x18 + fileNameBytes.Length));
        BigEndian.WriteUInt32(bytes, 0x300, 0x01000000);
        BigEndian.WriteUInt32(bytes, 0x304, 0);
        BigEndian.WriteUInt32(bytes, 0x308, 2);
        BigEndian.WriteUInt32(bytes, 0x30c, 0);
        BigEndian.WriteUInt32(bytes, 0x310, 0x500);
        BigEndian.WriteUInt32(bytes, 0x314, (uint)fsysBytes.Length);
        fileNameBytes.CopyTo(bytes, 0x318);
        fsysBytes.CopyTo(bytes, 0x500);
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);
        var entry = Assert.Single(context.Iso!.Files, file => file.Name == "sample.fsys");

        var result = context.ExportIsoFile(entry, extractFsysContents: true, decode: false);

        var rawPath = Path.Combine(temp, "Sample CM Tool", "Game Files", "sample", "sample.fsys");
        var innerPath = Path.Combine(temp, "Sample CM Tool", "Game Files", "sample", "sample.msg");
        Assert.Equal(rawPath, result.FilePath);
        Assert.Contains(innerPath, result.ExtractedFiles);
        Assert.Equal([1, 2, 3], File.ReadAllBytes(innerPath));
    }

    [Fact]
    public void ImportsWorkspaceFileIntoIsoAndUpdatesTocSize()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var bytes = CreateSingleFileIso("sample.bin", [1, 2, 3, 4, 5, 6, 7, 8], fileSlotSize: 0x40);
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);
        var entry = Assert.Single(context.Iso!.Files, file => file.Name == "sample.bin");
        var workspacePath = context.ExtractIsoFile(entry);
        File.WriteAllBytes(workspacePath, [9, 8, 7]);

        var result = context.ImportIsoFile(entry, encode: false);

        Assert.Equal(3, result.WrittenBytes);
        var updatedIso = File.ReadAllBytes(isoPath);
        Assert.Equal([9, 8, 7, 0, 0, 0, 0, 0], updatedIso[0x500..0x508]);
        Assert.Equal((uint)3, ColosseumProjectContext.Open(isoPath).Iso!.Files.Single(file => file.Name == "sample.bin").Size);
    }

    [Fact]
    public void DeletesIsoFileWithLegacyPreservedMarker()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var bytes = CreateSingleFileIso("sample.bin", Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(), fileSlotSize: 0x40);
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);
        var entry = Assert.Single(context.Iso!.Files, file => file.Name == "sample.bin");

        var result = context.DeleteIsoFile(entry);

        Assert.Equal(16, result.WrittenBytes);
        Assert.True(File.Exists(result.BackupPath));
        var updatedIso = File.ReadAllBytes(isoPath);
        Assert.Equal("DELETED DELETED\0"u8.ToArray(), updatedIso[0x500..0x510]);
        Assert.Equal((uint)16, ColosseumProjectContext.Open(isoPath).Iso!.Files.Single(file => file.Name == "sample.bin").Size);
    }

    [Fact]
    public void EncodesMessageJsonAndPacksFsysWorkspaceFile()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var originalTable = GameStringTable.FromStrings([new GameString(7, "Old", 0)]).ToArray();
        var fsysBytes = CreateSingleEntryFsys(originalTable);
        var bytes = CreateSingleFileIso("sample.fsys", fsysBytes, fileSlotSize: 0x400);
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);
        var entry = Assert.Single(context.Iso!.Files, file => file.Name == "sample.fsys");
        var export = context.ExportIsoFile(entry, extractFsysContents: true, decode: true);
        var messagePath = Path.Combine(temp, "Sample CM Tool", "Game Files", "sample", "sample.msg");
        var jsonPath = messagePath + ".json";
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(new[] { new GameString(7, "New", 0) }));

        var result = context.EncodeIsoFile(entry);

        Assert.Equal(export.FilePath, result.FilePath);
        Assert.Contains(jsonPath, result.EncodedFiles);
        Assert.Contains(messagePath, result.PackedFiles);
        var archive = FsysArchive.Load(export.FilePath);
        var message = Assert.Single(archive.Entries);
        var table = GameStringTable.Parse(archive.Extract(message));
        Assert.Equal("New", table.StringWithId(7));
    }

    [Fact]
    public void DecodesAndRepacksPkxDatThroughFsysIsoExplorerFlow()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var originalDat = new byte[] { 1, 2, 3, 4, 5 };
        var fsysBytes = CreateFsys(("sample.pkx", CreatePkx(originalDat, [0xaa, 0xbb, 0xcc]), GameFileType.Pkx));
        var bytes = CreateSingleFileIso("models.fsys", fsysBytes, fileSlotSize: 0x500);
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);
        var entry = Assert.Single(context.Iso!.Files, file => file.Name == "models.fsys");

        var export = context.ExportIsoFile(entry, extractFsysContents: true, decode: true);

        var folder = Path.Combine(temp, "Sample CM Tool", "Game Files", "models");
        var pkxPath = Path.Combine(folder, "sample.pkx");
        var datPath = pkxPath + ".dat";
        Assert.Contains(datPath, export.DecodedFiles);
        Assert.Equal(originalDat, File.ReadAllBytes(datPath));

        var replacementDat = new byte[] { 9, 8, 7, 6, 5, 4, 3 };
        File.WriteAllBytes(datPath, replacementDat);

        var result = context.EncodeIsoFile(entry);

        Assert.Contains(datPath, result.EncodedFiles);
        Assert.Contains(pkxPath, result.PackedFiles);
        var archive = FsysArchive.Load(export.FilePath);
        var archivedPkx = archive.Extract(Assert.Single(archive.Entries, file => file.Name == "sample.pkx"));
        Assert.True(ColosseumLegacyFileCodecs.TryExportColosseumPkxDat(archivedPkx, out var archivedDat));
        Assert.Equal(replacementDat, archivedDat);
        Assert.Equal([0xaa, 0xbb, 0xcc], archivedPkx[^3..]);
    }

    [Fact]
    public void DecodesAndRepacksWzxEmbeddedModelsThroughFsysIsoExplorerFlow()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var originalModel = CreateDatModel(0x40, fill: 0x20);
        var fsysBytes = CreateFsys(("sample.wzx", CreateWzx(originalModel), GameFileType.Wzx));
        var bytes = CreateSingleFileIso("models.fsys", fsysBytes, fileSlotSize: 0x500);
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);
        var entry = Assert.Single(context.Iso!.Files, file => file.Name == "models.fsys");

        var export = context.ExportIsoFile(entry, extractFsysContents: true, decode: true);

        var folder = Path.Combine(temp, "Sample CM Tool", "Game Files", "models");
        var wzxPath = Path.Combine(folder, "sample.wzx");
        var modelPath = Path.Combine(folder, "sample_0.wzx.dat");
        Assert.Contains(modelPath, export.DecodedFiles);
        Assert.Equal(originalModel, File.ReadAllBytes(modelPath));

        var replacementModel = CreateDatModel(0x40, fill: 0x50);
        File.WriteAllBytes(modelPath, replacementModel);

        var result = context.EncodeIsoFile(entry);

        Assert.Contains(modelPath, result.EncodedFiles);
        Assert.Contains(wzxPath, result.PackedFiles);
        var archive = FsysArchive.Load(export.FilePath);
        var archivedWzx = archive.Extract(Assert.Single(archive.Entries, file => file.Name == "sample.wzx"));
        var archivedModel = Assert.Single(ColosseumLegacyFileCodecs.ExtractWzxDatModels(archivedWzx));
        Assert.Equal(replacementModel, archivedModel.Data);
    }

    [Fact]
    public void CombinesAndSplitsThpHeaderBodyThroughFsysIsoExplorerFlow()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var thp = CreateThp();
        Assert.True(ColosseumLegacyFileCodecs.TrySplitThp(thp, out var thh, out var thd));
        var fsysBytes = CreateFsys(
            ("movie.thh", thh, GameFileType.Thh),
            ("movie.thd", thd, GameFileType.Thd));
        var bytes = CreateSingleFileIso("media.fsys", fsysBytes, fileSlotSize: 0x500);
        File.WriteAllBytes(isoPath, bytes);

        var context = ColosseumProjectContext.Open(isoPath);
        var entry = Assert.Single(context.Iso!.Files, file => file.Name == "media.fsys");

        var export = context.ExportIsoFile(entry, extractFsysContents: true, decode: true);

        var folder = Path.Combine(temp, "Sample CM Tool", "Game Files", "media");
        var thhPath = Path.Combine(folder, "movie.thh");
        var thdPath = Path.Combine(folder, "movie.thd");
        var thpPath = Path.Combine(folder, "movie.thp");
        Assert.Contains(thpPath, export.DecodedFiles);
        Assert.Equal(thp, File.ReadAllBytes(thpPath));

        var editedThp = thp.ToArray();
        editedThp[0x60] = 0xbe;
        editedThp[0x61] = 0xef;
        File.WriteAllBytes(thpPath, editedThp);

        var result = context.EncodeIsoFile(entry);

        Assert.Contains(thpPath, result.EncodedFiles);
        Assert.Contains(thhPath, result.PackedFiles);
        Assert.Contains(thdPath, result.PackedFiles);
        var archive = FsysArchive.Load(export.FilePath);
        var archivedHeader = archive.Extract(Assert.Single(archive.Entries, file => file.Name == "movie.thh"));
        var archivedBody = archive.Extract(Assert.Single(archive.Entries, file => file.Name == "movie.thd"));
        Assert.Equal(editedThp, ColosseumLegacyFileCodecs.CombineThp(archivedHeader, archivedBody));
    }

    [Fact]
    public void ImportsLargerFileByShiftingLaterIsoFiles()
    {
        var temp = Path.Combine(Path.GetTempPath(), "CipherSnagemEditorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var isoPath = Path.Combine(temp, "Sample.iso");
        var bytes = CreateTwoFileIso(
            "first.bin",
            [1, 2, 3, 4],
            "second.bin",
            [0x55, 0x66, 0x77, 0x88]);
        File.WriteAllBytes(isoPath, bytes);
        var originalLength = bytes.Length;

        var context = ColosseumProjectContext.Open(isoPath);
        var first = Assert.Single(context.Iso!.Files, file => file.Name == "first.bin");
        var workspacePath = context.ExtractIsoFile(first);
        var replacement = Enumerable.Range(0xa0, 0x30).Select(value => (byte)value).ToArray();
        File.WriteAllBytes(workspacePath, replacement);

        var result = context.ImportIsoFile(first, encode: false);

        Assert.Equal(0x20, result.InsertedBytes);
        Assert.Equal(0x30, result.WrittenBytes);
        var updatedIso = File.ReadAllBytes(isoPath);
        Assert.Equal(originalLength + 0x20, updatedIso.Length);
        Assert.Equal(replacement, updatedIso[0x500..0x530]);
        Assert.Equal([0x55, 0x66, 0x77, 0x88], updatedIso[0x530..0x534]);
        Assert.Equal((uint)(updatedIso.Length - 0x500), BigEndian.ReadUInt32(updatedIso, 0x438));

        var reopened = ColosseumProjectContext.Open(isoPath);
        Assert.Equal((uint)0x30, reopened.Iso!.Files.Single(file => file.Name == "first.bin").Size);
        Assert.Equal((uint)0x530, reopened.Iso!.Files.Single(file => file.Name == "second.bin").Offset);
    }

    private static byte[] CreateSingleFileIso(string fileName, byte[] fileBytes, int fileSlotSize)
    {
        var bytes = new byte[0x500 + fileSlotSize + 0x100];
        bytes[0] = (byte)'G';
        bytes[1] = (byte)'C';
        bytes[2] = (byte)'6';
        bytes[3] = (byte)'E';

        var fileNameBytes = System.Text.Encoding.ASCII.GetBytes(fileName + "\0");
        BigEndian.WriteUInt32(bytes, 0x420, 0x100);
        BigEndian.WriteUInt32(bytes, 0x424, 0x300);
        BigEndian.WriteUInt32(bytes, 0x428, (uint)(0x18 + fileNameBytes.Length));
        BigEndian.WriteUInt32(bytes, 0x434, 0x500);
        BigEndian.WriteUInt32(bytes, 0x438, (uint)(bytes.Length - 0x500));
        BigEndian.WriteUInt32(bytes, 0x300, 0x01000000);
        BigEndian.WriteUInt32(bytes, 0x304, 0);
        BigEndian.WriteUInt32(bytes, 0x308, 2);
        BigEndian.WriteUInt32(bytes, 0x30c, 0);
        BigEndian.WriteUInt32(bytes, 0x310, 0x500);
        BigEndian.WriteUInt32(bytes, 0x314, (uint)fileBytes.Length);
        fileNameBytes.CopyTo(bytes, 0x318);
        fileBytes.CopyTo(bytes, 0x500);
        return bytes;
    }

    private static byte[] CreateTwoFileIso(string firstName, byte[] firstBytes, string secondName, byte[] secondBytes)
    {
        var bytes = new byte[0x700];
        bytes[0] = (byte)'G';
        bytes[1] = (byte)'C';
        bytes[2] = (byte)'6';
        bytes[3] = (byte)'E';

        var firstNameBytes = System.Text.Encoding.ASCII.GetBytes(firstName + "\0");
        var secondNameBytes = System.Text.Encoding.ASCII.GetBytes(secondName + "\0");
        var stringTableLength = firstNameBytes.Length + secondNameBytes.Length;
        BigEndian.WriteUInt32(bytes, 0x420, 0x100);
        BigEndian.WriteUInt32(bytes, 0x424, 0x300);
        BigEndian.WriteUInt32(bytes, 0x428, (uint)(0x24 + stringTableLength));
        BigEndian.WriteUInt32(bytes, 0x434, 0x500);
        BigEndian.WriteUInt32(bytes, 0x438, (uint)(bytes.Length - 0x500));

        BigEndian.WriteUInt32(bytes, 0x300, 0x01000000);
        BigEndian.WriteUInt32(bytes, 0x304, 0);
        BigEndian.WriteUInt32(bytes, 0x308, 3);
        BigEndian.WriteUInt32(bytes, 0x30c, 0);
        BigEndian.WriteUInt32(bytes, 0x310, 0x500);
        BigEndian.WriteUInt32(bytes, 0x314, (uint)firstBytes.Length);
        BigEndian.WriteUInt32(bytes, 0x318, (uint)firstNameBytes.Length);
        BigEndian.WriteUInt32(bytes, 0x31c, 0x510);
        BigEndian.WriteUInt32(bytes, 0x320, (uint)secondBytes.Length);
        firstNameBytes.CopyTo(bytes, 0x324);
        secondNameBytes.CopyTo(bytes, 0x324 + firstNameBytes.Length);
        firstBytes.CopyTo(bytes, 0x500);
        secondBytes.CopyTo(bytes, 0x510);
        return bytes;
    }

    private static byte[] CreateSingleEntryFsys(byte[]? fileBytes = null)
        => CreateFsys(("sample.msg", fileBytes ?? [1, 2, 3], GameFileType.Message));

    private static byte[] CreateFsys(params (string FileName, byte[] FileBytes, GameFileType FileType)[] entries)
    {
        if (entries.Length == 0)
        {
            throw new ArgumentException("At least one FSYS entry is required.", nameof(entries));
        }

        const int pointerTableStart = 0x60;
        const int detailsStart = 0x80;
        const int detailsSize = 0x30;
        var namesStart = Align16(detailsStart + (entries.Length * detailsSize));
        var nameOffsets = new int[entries.Length];
        var dataOffsets = new int[entries.Length];

        var cursor = namesStart;
        for (var index = 0; index < entries.Length; index++)
        {
            nameOffsets[index] = cursor;
            cursor += System.Text.Encoding.ASCII.GetByteCount(entries[index].FileName) + 1;
        }

        cursor = Align16(cursor);
        for (var index = 0; index < entries.Length; index++)
        {
            dataOffsets[index] = cursor;
            cursor = Align16(cursor + entries[index].FileBytes.Length);
        }

        var bytes = new byte[Align16(cursor + 4)];
        BigEndian.WriteUInt32(bytes, 0x00, 0x46535953);
        BigEndian.WriteUInt32(bytes, 0x0c, checked((uint)entries.Length));
        BigEndian.WriteUInt32(bytes, 0x20, (uint)bytes.Length);

        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            var detailsOffset = detailsStart + (index * detailsSize);
            BigEndian.WriteUInt32(bytes, pointerTableStart + (index * 4), checked((uint)detailsOffset));
            BigEndian.WriteUInt32(bytes, detailsOffset + 0x00, checked((uint)entry.FileType << 8));
            BigEndian.WriteUInt32(bytes, detailsOffset + 0x04, checked((uint)dataOffsets[index]));
            BigEndian.WriteUInt32(bytes, detailsOffset + 0x08, checked((uint)entry.FileBytes.Length));
            BigEndian.WriteUInt32(bytes, detailsOffset + 0x14, checked((uint)entry.FileBytes.Length));
            BigEndian.WriteUInt32(bytes, detailsOffset + 0x24, checked((uint)nameOffsets[index]));

            var fileNameBytes = System.Text.Encoding.ASCII.GetBytes(entry.FileName + "\0");
            fileNameBytes.CopyTo(bytes.AsSpan(nameOffsets[index]));
            entry.FileBytes.CopyTo(bytes.AsSpan(dataOffsets[index]));
        }

        return bytes;
    }

    private static byte[] CreatePkx(byte[] dat, byte[] trailer)
    {
        var paddedDatLength = Align16(dat.Length);
        var pkx = new byte[0x40 + paddedDatLength + trailer.Length];
        BigEndian.WriteUInt32(pkx, 0, checked((uint)dat.Length));
        dat.CopyTo(pkx.AsSpan(0x40));
        trailer.CopyTo(pkx.AsSpan(0x40 + paddedDatLength));
        return pkx;
    }

    private static byte[] CreateWzx(byte[] model)
    {
        var wzx = new byte[0x10 + model.Length + 0x20];
        BigEndian.WriteUInt32(wzx, 0x08, checked((uint)model.Length));
        model.CopyTo(wzx.AsSpan(0x10));
        return wzx;
    }

    private static byte[] CreateDatModel(int length, byte fill)
    {
        var model = Enumerable.Repeat(fill, length).ToArray();
        BigEndian.WriteUInt32(model, 0, checked((uint)length));
        model[12] = 0;
        model[13] = 0;
        model[14] = 0;
        model[15] = 1;
        Array.Clear(model, 16, 16);
        "scene_data\0"u8.CopyTo(model.AsSpan(length - 16));
        return model;
    }

    private static byte[] CreateThp()
    {
        var thp = new byte[0x70];
        BigEndian.WriteUInt32(thp, 0x20, 0x30);
        BigEndian.WriteUInt32(thp, 0x28, 0x64);
        BigEndian.WriteUInt32(thp, 0x2c, 0x68);
        thp.AsSpan(0x34, 16).Fill(0xff);
        thp[0x34] = 0;
        thp[0x35] = 1;
        thp[0x60] = 0xde;
        thp[0x61] = 0xad;
        return thp;
    }

    private static int Align16(int value)
        => (value + 0x0f) & ~0x0f;
}
