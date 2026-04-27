using OrreForge.Core.Binary;

namespace OrreForge.Core.Relocation;

public sealed class RelocationTable
{
    private const int NumberOfSectionsOffset = 0x0c;
    private const int SectionInfoTableOffset = 0x10;
    private const int RelocationTableOffset = 0x24;
    private const int SectionInfoSize = 8;
    private const int RelocationCommandSize = 8;
    private const byte StopCommand = 203;

    private readonly BinaryData _data;
    private readonly Dictionary<int, RelocationPointer> _pointers = [];
    private readonly Dictionary<int, int> _symbolLengths = [];

    private RelocationTable(byte[] bytes, IReadOnlyDictionary<int, RelocationSection> sections)
    {
        Data = bytes;
        _data = new BinaryData(bytes);
        Sections = sections;
        ParsePointers();
    }

    public byte[] Data { get; }

    public IReadOnlyDictionary<int, RelocationSection> Sections { get; }

    public IReadOnlyDictionary<int, RelocationPointer> Pointers => _pointers;

    public IReadOnlyDictionary<int, int> SymbolLengths => _symbolLengths;

    public static RelocationTable Parse(byte[] bytes)
    {
        var data = new BinaryData(bytes);
        var sectionCount = checked((int)data.ReadUInt32(NumberOfSectionsOffset));
        var firstSectionInfoOffset = checked((int)data.ReadUInt32(SectionInfoTableOffset));
        var sections = new Dictionary<int, RelocationSection>();

        for (var index = 0; index < sectionCount; index++)
        {
            var sectionInfoOffset = firstSectionInfoOffset + (index * SectionInfoSize);
            if (sectionInfoOffset + SectionInfoSize > data.Length)
            {
                throw new InvalidDataException("REL section info table extends past the end of the file.");
            }

            var rawEntry = data.ReadUInt32(sectionInfoOffset);
            var dataOffset = checked((int)(rawEntry & 0xffff_fffc));
            var isTextSection = (rawEntry & 1) == 1;
            var length = checked((int)data.ReadUInt32(sectionInfoOffset + 4));

            sections[index] = new RelocationSection(index, sectionInfoOffset, dataOffset, isTextSection, length);
        }

        return new RelocationTable(bytes, sections);
    }

    public int GetPointer(int index)
        => _pointers.TryGetValue(index, out var pointer) ? pointer.DataOffset : -1;

    public int GetSymbolLength(int index)
        => _symbolLengths.TryGetValue(index, out var length) ? length : 0;

    public int GetValueAtPointer(int index)
    {
        var pointer = GetPointer(index);
        return pointer < 0 ? 0 : checked((int)_data.ReadUInt32(pointer));
    }

    public byte[] ReadSymbol(int index)
    {
        var pointer = GetPointer(index);
        var length = GetSymbolLength(index);
        if (pointer < 0 || length <= 0)
        {
            return [];
        }

        return _data.ReadBytes(pointer, length);
    }

    private void ParsePointers()
    {
        var currentOffset = checked((int)_data.ReadUInt32(RelocationTableOffset));
        var currentPointerId = 0;

        while (currentOffset <= _data.Length - RelocationCommandSize)
        {
            var command = _data.ReadByte(currentOffset + 2);
            if (command == StopCommand)
            {
                break;
            }

            var sectionId = _data.ReadByte(currentOffset + 3);
            if (!Sections.TryGetValue(sectionId, out var section))
            {
                throw new InvalidDataException($"REL relocation command references missing section {sectionId}.");
            }

            var symbolOffset = checked((int)_data.ReadUInt32(currentOffset + 4));
            var fileOffset = section.DataOffset + symbolOffset;

            if (command is > 0 and <= 13)
            {
                var existingId = IdForSymbol(fileOffset);
                if (existingId is not null)
                {
                    var existing = _pointers[existingId.Value];
                    _pointers[existingId.Value] = existing with
                    {
                        Addresses = existing.Addresses.Concat([currentOffset + 4]).ToArray()
                    };
                }
                else
                {
                    _pointers[currentPointerId] = new RelocationPointer(
                        currentPointerId,
                        sectionId,
                        [currentOffset + 4],
                        fileOffset);
                    currentPointerId++;
                }
            }

            currentOffset += RelocationCommandSize;
        }

        ComputeSymbolLengths();
    }

    private void ComputeSymbolLengths()
    {
        foreach (var (sectionId, section) in Sections)
        {
            if (section.Length <= 0 || section.IsTextSection)
            {
                continue;
            }

            var pointersInSection = _pointers
                .Values
                .Where(pointer => pointer.Section == sectionId)
                .OrderBy(pointer => pointer.DataOffset)
                .ToArray();
            if (pointersInSection.Length == 0)
            {
                continue;
            }

            for (var index = 0; index < pointersInSection.Length; index++)
            {
                var current = pointersInSection[index];
                var nextOffset = index + 1 < pointersInSection.Length
                    ? pointersInSection[index + 1].DataOffset
                    : section.DataOffset + section.Length;
                _symbolLengths[current.Index] = Math.Max(0, nextOffset - current.DataOffset);
            }
        }
    }

    private int? IdForSymbol(int dataOffset)
    {
        foreach (var (id, pointer) in _pointers)
        {
            if (pointer.DataOffset == dataOffset)
            {
                return id;
            }
        }

        return null;
    }
}
