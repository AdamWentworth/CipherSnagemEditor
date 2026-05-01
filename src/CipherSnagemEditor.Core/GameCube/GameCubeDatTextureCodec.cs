using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Core.GameCube;

public sealed record GameCubeDatTexture(int Index, byte[] TextureBytes);

public static class GameCubeDatTextureCodec
{
    private const int DatHeaderLength = 0x20;
    private const int TextureImageOffset = 0x4c;
    private const int TexturePaletteOffset = 0x50;

    public static IReadOnlyList<GameCubeDatTexture> ExtractTextures(byte[] dat)
    {
        var scanner = DatTextureScanner.TryCreate(dat);
        if (scanner is null)
        {
            return [];
        }

        var textures = new List<GameCubeDatTexture>();
        foreach (var reference in scanner.FindTextureReferences())
        {
            if (!IsReadableRange(dat, reference.DataOffset, reference.DataLength))
            {
                continue;
            }

            var pixelBytes = dat.AsSpan(reference.DataOffset, reference.DataLength).ToArray();
            var paletteBytes = reference.PaletteOffset is int paletteOffset && reference.PaletteEntries > 0
                ? dat.AsSpan(paletteOffset, reference.PaletteEntries * 2).ToArray()
                : [];

            if (GameCubeTextureCodec.TryCreateModelTexture(
                reference.Width,
                reference.Height,
                reference.StandardFormat,
                reference.PaletteFormatId ?? 0,
                pixelBytes,
                paletteBytes,
                out var textureBytes))
            {
                textures.Add(new GameCubeDatTexture(reference.Index, textureBytes));
            }
        }

        return textures;
    }

    public static bool TryImportTextures(
        byte[] dat,
        IReadOnlyDictionary<int, byte[]> textureReplacements,
        out byte[] importedDat,
        out int importedCount)
    {
        importedDat = dat.ToArray();
        importedCount = 0;
        var scanner = DatTextureScanner.TryCreate(dat);
        if (scanner is null)
        {
            return false;
        }

        foreach (var reference in scanner.FindTextureReferences())
        {
            if (!textureReplacements.TryGetValue(reference.Index, out var replacement))
            {
                continue;
            }

            if (!GameCubeTextureCodec.TryGetPayload(
                replacement,
                reference.PaletteEntries > 0 ? reference.PaletteEntries : null,
                out var payload))
            {
                continue;
            }

            if (payload.PixelBytes.Length != reference.DataLength
                || !IsReadableRange(importedDat, reference.DataOffset, reference.DataLength))
            {
                continue;
            }

            payload.PixelBytes.CopyTo(importedDat.AsSpan(reference.DataOffset, reference.DataLength));

            if (reference.PaletteOffset is int paletteOffset && reference.PaletteEntries > 0)
            {
                var paletteLength = reference.PaletteEntries * 2;
                if (payload.PaletteBytes.Length >= paletteLength && IsReadableRange(importedDat, paletteOffset, paletteLength))
                {
                    payload.PaletteBytes.AsSpan(0, paletteLength).CopyTo(importedDat.AsSpan(paletteOffset, paletteLength));
                }
            }

            importedCount++;
        }

        return importedCount > 0;
    }

    private static bool IsReadableRange(byte[] bytes, int offset, int length)
        => offset >= 0 && length >= 0 && offset <= bytes.Length - length;

    private sealed record TextureReference(
        int Index,
        int Width,
        int Height,
        int StandardFormat,
        int DataOffset,
        int DataLength,
        int? PaletteFormatId,
        int? PaletteOffset,
        int PaletteEntries);

    private sealed record PaletteReference(int FormatId, int Offset, int EntryCount);

    private sealed class DatTextureScanner
    {
        private readonly byte[] _dat;
        private readonly ReadOnlyMemory<byte> _model;
        private readonly int _rootNodesOffset;
        private readonly int _rootNodesCount;
        private readonly int _stringsOffset;
        private readonly List<TextureReference> _references = [];
        private readonly HashSet<int> _seenTextureDataOffsets = [];
        private readonly HashSet<int> _visitedScenes = [];
        private readonly HashSet<int> _visitedModelSets = [];
        private readonly HashSet<int> _visitedJoints = [];
        private readonly HashSet<int> _visitedMeshes = [];
        private readonly HashSet<int> _visitedMaterialObjects = [];
        private readonly HashSet<int> _visitedTextures = [];
        private readonly HashSet<int> _visitedAnimatedMaterialJoints = [];
        private readonly HashSet<int> _visitedAnimatedMaterials = [];
        private readonly HashSet<int> _visitedAnimatedTextures = [];

        private DatTextureScanner(byte[] dat, int rootNodesOffset, int rootNodesCount, int stringsOffset)
        {
            _dat = dat;
            _model = dat.AsMemory(DatHeaderLength);
            _rootNodesOffset = rootNodesOffset;
            _rootNodesCount = rootNodesCount;
            _stringsOffset = stringsOffset;
        }

        public static DatTextureScanner? TryCreate(byte[] dat)
        {
            if (dat.Length < DatHeaderLength)
            {
                return null;
            }

            var dataSize = BigEndian.ReadUInt32(dat, 0x04);
            var nodeCount = BigEndian.ReadUInt32(dat, 0x08);
            var publicRootNodes = BigEndian.ReadUInt32(dat, 0x0c);
            var externalRootNodes = BigEndian.ReadUInt32(dat, 0x10);
            var rootNodesOffset = (long)dataSize + (nodeCount * 4L);
            var rootNodesCount = (long)publicRootNodes + externalRootNodes;
            if (rootNodesCount <= 0
                || rootNodesCount > int.MaxValue
                || rootNodesOffset <= 0
                || rootNodesOffset > int.MaxValue
                || rootNodesOffset + (rootNodesCount * 8L) > dat.Length - DatHeaderLength)
            {
                return null;
            }

            return new DatTextureScanner(
                dat,
                (int)rootNodesOffset,
                (int)rootNodesCount,
                checked((int)(rootNodesOffset + (rootNodesCount * 8L))));
        }

        public IReadOnlyList<TextureReference> FindTextureReferences()
        {
            var foundNamedScene = false;
            for (var i = 0; i < _rootNodesCount; i++)
            {
                var rootOffset = _rootNodesOffset + i * 8;
                if (!string.Equals(ReadRootNodeName(rootOffset), "scene_data", StringComparison.Ordinal))
                {
                    continue;
                }

                foundNamedScene = true;
                var pointer = ReadPointer(rootOffset);
                ParseSceneData(pointer);
            }

            if (foundNamedScene)
            {
                return _references;
            }

            for (var i = 0; i < _rootNodesCount; i++)
            {
                ParseSceneData(ReadPointer(_rootNodesOffset + i * 8));
            }

            return _references;
        }

        private void ParseSceneData(int offset)
        {
            if (!TryVisit(_visitedScenes, offset, 16))
            {
                return;
            }

            ParseModelSetsArray(ReadPointer(offset));
        }

        private void ParseModelSetsArray(int offset)
        {
            if (!Validate(offset, 4))
            {
                return;
            }

            for (var current = offset; Validate(current, 4) && current - offset < 0x2000; current += 4)
            {
                var pointer = ReadPointer(current);
                if (!Validate(pointer, 16))
                {
                    break;
                }

                ParseModelSet(pointer);
            }
        }

        private void ParseModelSet(int offset)
        {
            if (!TryVisit(_visitedModelSets, offset, 16))
            {
                return;
            }

            ParseJoint(ReadPointer(offset));
            ParseAnimatedMaterialJoint(ReadPointer(offset + 8));
        }

        private void ParseJoint(int offset)
        {
            if (!TryVisit(_visitedJoints, offset, 64))
            {
                return;
            }

            ParseJoint(ReadPointer(offset + 8));
            ParseJoint(ReadPointer(offset + 12));
            ParseMesh(ReadPointer(offset + 16));
        }

        private void ParseMesh(int offset)
        {
            if (!TryVisit(_visitedMeshes, offset, 16))
            {
                return;
            }

            ParseMesh(ReadPointer(offset + 4));
            ParseMaterialObject(ReadPointer(offset + 8));
        }

        private void ParseMaterialObject(int offset)
        {
            if (!TryVisit(_visitedMaterialObjects, offset, 24))
            {
                return;
            }

            ParseTexture(ReadPointer(offset + 8));
        }

        private void ParseTexture(int offset)
        {
            if (!TryVisit(_visitedTextures, offset, 92))
            {
                return;
            }

            ParseTexture(ReadPointer(offset + 4));
            var palette = ReadPalette(ReadPointer(offset + TexturePaletteOffset));
            AddImage(ReadPointer(offset + TextureImageOffset), palette);
        }

        private void ParseAnimatedMaterialJoint(int offset)
        {
            if (!TryVisit(_visitedAnimatedMaterialJoints, offset, 12))
            {
                return;
            }

            ParseAnimatedMaterialJoint(ReadPointer(offset));
            ParseAnimatedMaterialJoint(ReadPointer(offset + 4));
            ParseAnimatedMaterial(ReadPointer(offset + 8));
        }

        private void ParseAnimatedMaterial(int offset)
        {
            if (!TryVisit(_visitedAnimatedMaterials, offset, 16))
            {
                return;
            }

            ParseAnimatedMaterial(ReadPointer(offset));
            ParseAnimatedTexture(ReadPointer(offset + 8));
        }

        private void ParseAnimatedTexture(int offset)
        {
            if (!TryVisit(_visitedAnimatedTextures, offset, 24))
            {
                return;
            }

            ParseAnimatedTexture(ReadPointer(offset));
            var imageArray = ReadPointer(offset + 12);
            var paletteArray = ReadPointer(offset + 16);
            var imageCount = ReadUInt16(offset + 20);
            var paletteCount = ReadUInt16(offset + 22);
            var palettes = new List<PaletteReference?>();
            for (var i = 0; i < paletteCount && i < 512; i++)
            {
                if (!Validate(paletteArray + i * 4, 4))
                {
                    break;
                }

                palettes.Add(ReadPalette(ReadPointer(paletteArray + i * 4)));
            }

            for (var i = 0; i < imageCount && i < 512; i++)
            {
                if (!Validate(imageArray + i * 4, 4))
                {
                    break;
                }

                AddImage(ReadPointer(imageArray + i * 4), i < palettes.Count ? palettes[i] : null);
            }
        }

        private PaletteReference? ReadPalette(int offset)
        {
            if (!Validate(offset, 14))
            {
                return null;
            }

            var dataOffset = ReadPointer(offset);
            var formatValue = ReadUInt32(offset + 4);
            if (formatValue > int.MaxValue)
            {
                return null;
            }

            var format = (int)formatValue;
            var entries = ReadUInt16(offset + 12);
            if (entries <= 0 || !Validate(dataOffset, entries * 2))
            {
                return null;
            }

            return new PaletteReference(format, DatHeaderLength + dataOffset, entries);
        }

        private void AddImage(int offset, PaletteReference? palette)
        {
            if (!Validate(offset, 16))
            {
                return;
            }

            var dataPointer = ReadPointer(offset);
            if (!_seenTextureDataOffsets.Add(dataPointer))
            {
                return;
            }

            var width = ReadUInt16(offset + 4);
            var height = ReadUInt16(offset + 6);
            var standardFormatValue = ReadUInt32(offset + 8);
            if (standardFormatValue > int.MaxValue)
            {
                return;
            }

            var standardFormat = (int)standardFormatValue;
            if (!TryExpectedTextureLength(width, height, standardFormat, out var dataLength)
                || !Validate(dataPointer, dataLength))
            {
                return;
            }

            _references.Add(new TextureReference(
                _references.Count,
                width,
                height,
                standardFormat,
                DatHeaderLength + dataPointer,
                dataLength,
                palette?.FormatId,
                palette?.Offset,
                palette?.EntryCount ?? 0));
        }

        private bool TryVisit(HashSet<int> visited, int offset, int minimumLength)
        {
            if (!Validate(offset, minimumLength))
            {
                return false;
            }

            return visited.Add(offset);
        }

        private bool Validate(int offset, int length)
            => offset > 0 && length >= 0 && offset <= _model.Length - length;

        private int ReadPointer(int offset)
        {
            if (!Validate(offset, 4))
            {
                return 0;
            }

            var pointer = ReadUInt32(offset);
            return pointer <= int.MaxValue ? (int)pointer : 0;
        }

        private string ReadRootNodeName(int rootOffset)
        {
            var nameOffset = _stringsOffset + ReadPointer(rootOffset + 4);
            if (!Validate(nameOffset, 1))
            {
                return string.Empty;
            }

            var chars = new List<byte>();
            for (var offset = nameOffset; Validate(offset, 1) && chars.Count < 128; offset++)
            {
                var value = _model.Span[offset];
                if (value == 0)
                {
                    break;
                }

                chars.Add(value);
            }

            return System.Text.Encoding.ASCII.GetString(chars.ToArray());
        }

        private ushort ReadUInt16(int offset)
            => Validate(offset, 2) ? BigEndian.ReadUInt16(_model.Span, offset) : (ushort)0;

        private uint ReadUInt32(int offset)
            => Validate(offset, 4) ? BigEndian.ReadUInt32(_model.Span, offset) : 0;
    }

    private static bool TryExpectedTextureLength(int width, int height, int standardFormat, out int length)
    {
        length = 0;
        if (width <= 0 || height <= 0 || !TryFormatInfo(standardFormat, out var blockWidth, out var blockHeight, out var bitsPerPixel))
        {
            return false;
        }

        var paddedWidth = Align(width, blockWidth);
        var paddedHeight = Align(height, blockHeight);
        var byteLength = (long)paddedWidth * paddedHeight * bitsPerPixel / 8;
        if (byteLength > int.MaxValue)
        {
            return false;
        }

        length = (int)byteLength;
        return true;
    }

    private static bool TryFormatInfo(int standardFormat, out int blockWidth, out int blockHeight, out int bitsPerPixel)
    {
        blockWidth = standardFormat is 3 or 4 or 5 or 6 or 10 ? 4 : 8;
        blockHeight = standardFormat is 0 or 8 or 14 ? 8 : 4;
        bitsPerPixel = standardFormat switch
        {
            0 or 8 or 14 => 4,
            1 or 2 or 9 => 8,
            3 or 4 or 5 or 10 => 16,
            6 => 32,
            _ => 0
        };
        return bitsPerPixel > 0;
    }

    private static int Align(int value, int alignment)
        => (value + alignment - 1) / alignment * alignment;
}
