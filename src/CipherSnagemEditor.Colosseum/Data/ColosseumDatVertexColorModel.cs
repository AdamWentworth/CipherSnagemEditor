using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Colosseum.Data;

public sealed class ColosseumDatVertexColorModel
{
    private const int ArchiveHeaderSize = 0x20;
    private const int VertexDescriptorSize = 0x18;
    private const int MaxArrayEntries = 100_000;

    private readonly byte[] _bytes;
    private readonly List<ColosseumDatVertexColor> _vertexColors;

    private ColosseumDatVertexColorModel(byte[] bytes, List<ColosseumDatVertexColor> vertexColors)
    {
        _bytes = bytes;
        _vertexColors = vertexColors;
    }

    public IReadOnlyList<ColosseumDatVertexColor> VertexColors => _vertexColors;

    public byte[] ToArray() => (byte[])_bytes.Clone();

    public static ColosseumDatVertexColorModel Load(string path)
        => Parse(File.ReadAllBytes(path));

    public static ColosseumDatVertexColorModel Parse(byte[] bytes)
    {
        var parser = new Parser(bytes);
        return new ColosseumDatVertexColorModel(bytes, parser.Parse());
    }

    public int ApplyFilter(ColosseumVertexColorFilter filter)
    {
        for (var index = 0; index < _vertexColors.Count; index++)
        {
            var filtered = ApplyFilter(_vertexColors[index], filter);
            _vertexColors[index] = filtered;
            WriteColor(filtered);
        }

        return _vertexColors.Count;
    }

    public void Save(string path)
        => File.WriteAllBytes(path, _bytes);

    public static string FilterName(ColosseumVertexColorFilter filter)
        => filter switch
        {
            ColosseumVertexColorFilter.MinorRedShift => "Minor Red Shift",
            ColosseumVertexColorFilter.RedScale => "Red Scale",
            ColosseumVertexColorFilter.PrimaryShift => "Primary Shift",
            ColosseumVertexColorFilter.ReversePrimaryShift => "Reverse Primary Shift",
            _ => "None"
        };

    public static ColosseumDatVertexColor ApplyFilter(ColosseumDatVertexColor color, ColosseumVertexColorFilter filter)
    {
        var red = color.Red;
        var green = color.Green;
        var blue = color.Blue;

        switch (filter)
        {
            case ColosseumVertexColorFilter.MinorRedShift:
                red = Clamp(red + 20);
                green = Clamp(green - 10);
                blue = Clamp(blue - 10);
                break;
            case ColosseumVertexColorFilter.RedScale:
                Span<int> ranks = stackalloc[] { red, green, blue };
                ranks.Sort();
                red = ranks[2];
                green = Clamp((ranks[0] + ranks[1]) / 2);
                blue = green;
                break;
            case ColosseumVertexColorFilter.PrimaryShift:
                (red, green, blue) = (green, blue, red);
                break;
            case ColosseumVertexColorFilter.ReversePrimaryShift:
                (red, green, blue) = (blue, red, green);
                break;
        }

        return color with
        {
            Red = Clamp(red),
            Green = Clamp(green),
            Blue = Clamp(blue),
            Alpha = Clamp(color.Alpha)
        };
    }

    private void WriteColor(ColosseumDatVertexColor color)
    {
        switch (color.Format)
        {
            case ColosseumDatVertexColorFormat.Rgb8:
                _bytes[color.FileOffset] = (byte)Clamp(color.Red);
                _bytes[color.FileOffset + 1] = (byte)Clamp(color.Green);
                _bytes[color.FileOffset + 2] = (byte)Clamp(color.Blue);
                break;
            case ColosseumDatVertexColorFormat.Rgbx8:
                _bytes[color.FileOffset] = (byte)Clamp(color.Red);
                _bytes[color.FileOffset + 1] = (byte)Clamp(color.Green);
                _bytes[color.FileOffset + 2] = (byte)Clamp(color.Blue);
                _bytes[color.FileOffset + 3] = (byte)(color.Extra & 0xff);
                break;
            case ColosseumDatVertexColorFormat.Rgba8:
                _bytes[color.FileOffset] = (byte)Clamp(color.Red);
                _bytes[color.FileOffset + 1] = (byte)Clamp(color.Green);
                _bytes[color.FileOffset + 2] = (byte)Clamp(color.Blue);
                _bytes[color.FileOffset + 3] = (byte)Clamp(color.Alpha);
                break;
            case ColosseumDatVertexColorFormat.Rgba4:
                BigEndian.WriteUInt16(
                    _bytes,
                    color.FileOffset,
                    (ushort)((Quantize(color.Red, 16, 0x0f) << 12)
                             | (Quantize(color.Green, 16, 0x0f) << 8)
                             | (Quantize(color.Blue, 16, 0x0f) << 4)
                             | Quantize(color.Alpha, 16, 0x0f)));
                break;
            case ColosseumDatVertexColorFormat.Rgba6:
                BigEndian.WriteUInt32(
                    _bytes,
                    color.FileOffset,
                    (uint)((Quantize(color.Red, 4, 0x3f) << 26)
                           | (Quantize(color.Green, 4, 0x3f) << 20)
                           | (Quantize(color.Blue, 4, 0x3f) << 14)
                           | (Quantize(color.Alpha, 4, 0x3f) << 8)
                           | (color.Extra & 0xff)));
                break;
            default:
                BigEndian.WriteUInt16(
                    _bytes,
                    color.FileOffset,
                    (ushort)((Quantize(color.Red, 8, 0x1f) << 11)
                             | (Quantize(color.Green, 4, 0x3f) << 5)
                             | Quantize(color.Blue, 8, 0x1f)));
                break;
        }
    }

    private static int Clamp(int value)
        => Math.Clamp(value, 0, 255);

    private static int Quantize(int value, int divisor, int max)
        => Math.Clamp(Clamp(value) / divisor, 0, max);

    private sealed class Parser
    {
        private readonly byte[] _bytes;
        private readonly int _modelLength;
        private readonly List<ColosseumDatVertexColor> _colors = [];
        private readonly HashSet<int> _colorOffsets = [];
        private readonly HashSet<int> _parsedColorLists = [];
        private readonly HashSet<int> _sceneData = [];
        private readonly HashSet<int> _modelSetsArrays = [];
        private readonly HashSet<int> _modelSets = [];
        private readonly HashSet<int> _joints = [];
        private readonly HashSet<int> _meshes = [];
        private readonly HashSet<int> _pObjects = [];
        private readonly HashSet<int> _shapeSets = [];
        private readonly HashSet<int> _vertexArrays = [];

        public Parser(byte[] bytes)
        {
            _bytes = bytes;
            _modelLength = Math.Max(0, bytes.Length - ArchiveHeaderSize);
        }

        public List<ColosseumDatVertexColor> Parse()
        {
            if (_bytes.Length < ArchiveHeaderSize || _modelLength <= 0)
            {
                return _colors;
            }

            if (!TryReadFileUInt32(0x04, out var dataSize)
                || !TryReadFileUInt32(0x08, out var nodeCount)
                || !TryReadFileUInt32(0x0c, out var publicRootCount)
                || !TryReadFileUInt32(0x10, out var externalRootCount))
            {
                return _colors;
            }

            var rootCount = publicRootCount + externalRootCount;
            var rootNodesOffset = dataSize + (nodeCount * 4);
            var stringsOffset = rootNodesOffset + (rootCount * 8);
            if (rootCount <= 0
                || !IsModelRange(rootNodesOffset, rootCount * 8)
                || !IsModelOffset(stringsOffset))
            {
                return _colors;
            }

            for (var index = 0; index < rootCount; index++)
            {
                var rootOffset = rootNodesOffset + (index * 8);
                if (!TryReadModelUInt32(rootOffset, out var pointer)
                    || !TryReadModelUInt32(rootOffset + 4, out var namePointer))
                {
                    continue;
                }

                var typeName = ReadModelString(stringsOffset + namePointer);
                if (typeName == "scene_data")
                {
                    ParseSceneData(pointer);
                }
            }

            return _colors
                .OrderBy(color => color.ModelOffset)
                .ToList();
        }

        private void ParseSceneData(int offset)
        {
            if (!Enter(_sceneData, offset) || !TryReadModelUInt32(offset, out var modelSetsPointer))
            {
                return;
            }

            ParseModelSetsArray(modelSetsPointer);
        }

        private void ParseModelSetsArray(int offset)
        {
            if (!Enter(_modelSetsArrays, offset))
            {
                return;
            }

            for (var current = offset; IsModelRange(current, 4); current += 4)
            {
                if ((current - offset) / 4 > MaxArrayEntries
                    || !TryReadModelUInt32(current, out var pointer)
                    || pointer == 0)
                {
                    break;
                }

                if (!IsModelOffset(pointer))
                {
                    break;
                }

                ParseModelSet(pointer);
            }
        }

        private void ParseModelSet(int offset)
        {
            if (!Enter(_modelSets, offset) || !TryReadModelUInt32(offset, out var jointPointer))
            {
                return;
            }

            ParseJoint(jointPointer);
        }

        private void ParseJoint(int offset)
        {
            if (!Enter(_joints, offset))
            {
                return;
            }

            if (TryReadModelUInt32(offset + 0x08, out var childJoint))
            {
                ParseJoint(childJoint);
            }

            if (TryReadModelUInt32(offset + 0x0c, out var nextJoint))
            {
                ParseJoint(nextJoint);
            }

            if (!TryReadModelUInt32(offset + 0x04, out var flags)
                || !TryReadModelUInt32(offset + 0x10, out var variablePointer)
                || variablePointer == 0)
            {
                return;
            }

            var isParticle = IsFlagSet(flags, 5);
            var isSpline = IsFlagSet(flags, 14);
            if (!isParticle && !isSpline)
            {
                ParseMesh(variablePointer);
            }
        }

        private void ParseMesh(int offset)
        {
            if (!Enter(_meshes, offset))
            {
                return;
            }

            if (TryReadModelUInt32(offset + 0x04, out var nextMesh))
            {
                ParseMesh(nextMesh);
            }

            if (TryReadModelUInt32(offset + 0x0c, out var pObject))
            {
                ParsePObject(pObject);
            }
        }

        private void ParsePObject(int offset)
        {
            if (!Enter(_pObjects, offset))
            {
                return;
            }

            if (TryReadModelUInt32(offset + 0x04, out var nextPObject))
            {
                ParsePObject(nextPObject);
            }

            if (TryReadModelUInt32(offset + 0x08, out var vertices))
            {
                ParseVertexArray(vertices);
            }

            if (!TryReadModelUInt16(offset + 0x0c, out var flags)
                || !TryReadModelUInt32(offset + 0x14, out var variableObject)
                || variableObject == 0)
            {
                return;
            }

            var variableType = (flags >> 12) & 3;
            if (variableType == 0)
            {
                ParseJoint(variableObject);
            }
            else if (variableType == 1)
            {
                ParseShapeSet(variableObject);
            }
        }

        private void ParseShapeSet(int offset)
        {
            if (!Enter(_shapeSets, offset))
            {
                return;
            }

            if (TryReadModelUInt32(offset + 0x08, out var vertices))
            {
                ParseVertexArray(vertices);
            }

            if (TryReadModelUInt32(offset + 0x14, out var normals))
            {
                ParseVertexArray(normals);
            }
        }

        private void ParseVertexArray(int offset)
        {
            if (!Enter(_vertexArrays, offset))
            {
                return;
            }

            for (var current = offset; IsModelRange(current, VertexDescriptorSize); current += VertexDescriptorSize)
            {
                if ((current - offset) / VertexDescriptorSize > MaxArrayEntries
                    || IsNullModelData(current, VertexDescriptorSize)
                    || !TryReadModelUInt32(current, out var attribute)
                    || attribute == 0xff)
                {
                    break;
                }

                if (attribute is not (11 or 12)
                    || !TryReadModelUInt32(current + 0x0c, out var componentType)
                    || !TryReadModelUInt16(current + 0x12, out var stride)
                    || !TryReadModelUInt32(current + 0x14, out var dataPointer)
                    || dataPointer == 0
                    || !_parsedColorLists.Add(dataPointer))
                {
                    continue;
                }

                ParseColorList(dataPointer, FormatFor(componentType), stride);
            }
        }

        private void ParseColorList(int offset, ColosseumDatVertexColorFormat format, int stride)
        {
            var colorLength = ColorLength(format);
            var step = Math.Max(stride, colorLength);
            if (step <= 0)
            {
                return;
            }

            for (var current = offset; IsModelRange(current, colorLength); current += step)
            {
                if ((current - offset) / step > MaxArrayEntries
                    || IsNullModelData(current, 4))
                {
                    break;
                }

                AddColor(current, format, stride);
            }
        }

        private void AddColor(int modelOffset, ColosseumDatVertexColorFormat format, int stride)
        {
            if (!_colorOffsets.Add(modelOffset))
            {
                return;
            }

            var fileOffset = ArchiveHeaderSize + modelOffset;
            var color = ReadColor(modelOffset, fileOffset, format, stride);
            if (color is not null)
            {
                _colors.Add(color);
            }
        }

        private ColosseumDatVertexColor? ReadColor(int modelOffset, int fileOffset, ColosseumDatVertexColorFormat format, int stride)
        {
            if (!IsFileRange(fileOffset, ColorLength(format)))
            {
                return null;
            }

            return format switch
            {
                ColosseumDatVertexColorFormat.Rgb8 => new ColosseumDatVertexColor(
                    modelOffset,
                    fileOffset,
                    format,
                    stride,
                    _bytes[fileOffset],
                    _bytes[fileOffset + 1],
                    _bytes[fileOffset + 2],
                    0xff,
                    0),
                ColosseumDatVertexColorFormat.Rgbx8 => new ColosseumDatVertexColor(
                    modelOffset,
                    fileOffset,
                    format,
                    stride,
                    _bytes[fileOffset],
                    _bytes[fileOffset + 1],
                    _bytes[fileOffset + 2],
                    0xff,
                    _bytes[fileOffset + 3]),
                ColosseumDatVertexColorFormat.Rgba8 => new ColosseumDatVertexColor(
                    modelOffset,
                    fileOffset,
                    format,
                    stride,
                    _bytes[fileOffset],
                    _bytes[fileOffset + 1],
                    _bytes[fileOffset + 2],
                    _bytes[fileOffset + 3],
                    0),
                ColosseumDatVertexColorFormat.Rgba4 => ReadRgba4(modelOffset, fileOffset, format, stride),
                ColosseumDatVertexColorFormat.Rgba6 => ReadRgba6(modelOffset, fileOffset, format, stride),
                _ => ReadRgb565(modelOffset, fileOffset, format, stride)
            };
        }

        private ColosseumDatVertexColor ReadRgba4(int modelOffset, int fileOffset, ColosseumDatVertexColorFormat format, int stride)
        {
            var raw = BigEndian.ReadUInt16(_bytes, fileOffset);
            return new ColosseumDatVertexColor(
                modelOffset,
                fileOffset,
                format,
                stride,
                ((raw >> 12) & 0x0f) * 16,
                ((raw >> 8) & 0x0f) * 16,
                ((raw >> 4) & 0x0f) * 16,
                (raw & 0x0f) * 16,
                0);
        }

        private ColosseumDatVertexColor ReadRgba6(int modelOffset, int fileOffset, ColosseumDatVertexColorFormat format, int stride)
        {
            var raw = BigEndian.ReadUInt32(_bytes, fileOffset);
            return new ColosseumDatVertexColor(
                modelOffset,
                fileOffset,
                format,
                stride,
                (int)((raw >> 26) & 0x3f) * 4,
                (int)((raw >> 20) & 0x3f) * 4,
                (int)((raw >> 14) & 0x3f) * 4,
                (int)((raw >> 8) & 0x3f) * 4,
                (int)(raw & 0xff));
        }

        private ColosseumDatVertexColor ReadRgb565(int modelOffset, int fileOffset, ColosseumDatVertexColorFormat format, int stride)
        {
            var raw = BigEndian.ReadUInt16(_bytes, fileOffset);
            return new ColosseumDatVertexColor(
                modelOffset,
                fileOffset,
                format,
                stride,
                ((raw >> 11) & 0x1f) * 8,
                ((raw >> 5) & 0x3f) * 4,
                (raw & 0x1f) * 8,
                0xff,
                0);
        }

        private bool Enter(HashSet<int> visited, int offset)
            => IsModelOffset(offset) && visited.Add(offset);

        private bool TryReadFileUInt32(int offset, out int value)
        {
            value = 0;
            if (!IsFileRange(offset, 4))
            {
                return false;
            }

            var raw = BigEndian.ReadUInt32(_bytes, offset);
            if (raw > int.MaxValue)
            {
                return false;
            }

            value = (int)raw;
            return true;
        }

        private bool TryReadModelUInt32(int offset, out int value)
        {
            value = 0;
            if (!IsModelRange(offset, 4))
            {
                return false;
            }

            var raw = BigEndian.ReadUInt32(_bytes, ArchiveHeaderSize + offset);
            if (raw > int.MaxValue)
            {
                return false;
            }

            value = (int)raw;
            return true;
        }

        private bool TryReadModelUInt16(int offset, out int value)
        {
            value = 0;
            if (!IsModelRange(offset, 2))
            {
                return false;
            }

            value = BigEndian.ReadUInt16(_bytes, ArchiveHeaderSize + offset);
            return true;
        }

        private string ReadModelString(int offset)
        {
            if (!IsModelOffset(offset))
            {
                return string.Empty;
            }

            var fileOffset = ArchiveHeaderSize + offset;
            var end = fileOffset;
            while (end < _bytes.Length && _bytes[end] != 0)
            {
                end++;
            }

            return System.Text.Encoding.ASCII.GetString(_bytes, fileOffset, end - fileOffset);
        }

        private bool IsNullModelData(int offset, int length)
        {
            if (!IsModelRange(offset, length))
            {
                return true;
            }

            var fileOffset = ArchiveHeaderSize + offset;
            for (var index = 0; index < length; index++)
            {
                if (_bytes[fileOffset + index] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsModelOffset(int offset)
            => offset > 0 && offset < _modelLength;

        private bool IsModelRange(int offset, int length)
            => offset > 0 && length >= 0 && offset <= _modelLength - length;

        private bool IsFileRange(int offset, int length)
            => offset >= 0 && length >= 0 && offset <= _bytes.Length - length;

        private static bool IsFlagSet(int flags, int nameIndex)
            => (flags & (1 << (31 - nameIndex))) != 0;

        private static ColosseumDatVertexColorFormat FormatFor(int componentType)
            => componentType switch
            {
                1 => ColosseumDatVertexColorFormat.Rgb8,
                2 => ColosseumDatVertexColorFormat.Rgbx8,
                3 => ColosseumDatVertexColorFormat.Rgba4,
                4 => ColosseumDatVertexColorFormat.Rgba6,
                5 => ColosseumDatVertexColorFormat.Rgba8,
                _ => ColosseumDatVertexColorFormat.Rgb565
            };

        private static int ColorLength(ColosseumDatVertexColorFormat format)
            => format switch
            {
                ColosseumDatVertexColorFormat.Rgb8 => 3,
                ColosseumDatVertexColorFormat.Rgba4 => 2,
                ColosseumDatVertexColorFormat.Rgb565 => 2,
                _ => 4
            };
    }
}
