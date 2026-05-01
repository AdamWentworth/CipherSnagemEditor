using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Core.GameCube;

public sealed record GameCubeGswTexture(int Id, byte[] TextureBytes);

public static class GameCubeGswTextureCodec
{
    private const int IdCountOffset = 0x18;
    private const int TextureHeaderRelativeOffset = 0x20;
    private const int IndexedC8MarkerRelativeOffset = 0x24;
    private const int FormatRelativeOffset = 0x28;
    private const int PaletteStartRelativeOffset = 0x68;
    private const int PaletteLength = 0x200;

    private static readonly HashSet<uint> TextureFormats =
    [
        0x00,
        0x01,
        0x30,
        0x40,
        0x41,
        0x42,
        0x43,
        0x44,
        0x45,
        0x90,
        0xb0
    ];

    public static IReadOnlyList<GameCubeGswTexture> ExtractTextures(byte[] gsw)
    {
        var sections = FindTextureSections(gsw);
        if (sections.Count == 0)
        {
            return [];
        }

        return sections
            .Select(section => new GameCubeGswTexture(section.Id, gsw.AsSpan(section.TextureOffset, section.Length).ToArray()))
            .ToArray();
    }

    public static bool TryImportTextures(
        byte[] gsw,
        IReadOnlyDictionary<int, byte[]> replacements,
        out byte[] importedGsw,
        out int importedCount)
    {
        importedGsw = gsw.ToArray();
        importedCount = 0;
        if (replacements.Count == 0)
        {
            return false;
        }

        foreach (var section in FindTextureSections(gsw))
        {
            if (!replacements.TryGetValue(section.Id, out var replacement)
                || replacement.Length != section.Length)
            {
                continue;
            }

            replacement.CopyTo(importedGsw.AsSpan(section.TextureOffset, section.Length));
            importedCount++;
        }

        return importedCount > 0;
    }

    private static IReadOnlyList<GswTextureSection> FindTextureSections(byte[] gsw)
    {
        if (gsw.Length <= IdCountOffset + 2)
        {
            return [];
        }

        var count = Math.Min((int)BigEndian.ReadUInt16(gsw, IdCountOffset), 0xff);
        var sections = new List<GswTextureSection>();
        for (var id = 1; id <= count; id++)
        {
            var section = FindTextureSection(gsw, id);
            if (section is not null)
            {
                sections.Add(section);
            }
        }

        return sections;
    }

    private static GswTextureSection? FindTextureSection(byte[] gsw, int id)
    {
        var marker = new byte[] { 0x03, 0x00, 0x00, checked((byte)id) };
        var searchOffset = 0;
        while (searchOffset <= gsw.Length - marker.Length)
        {
            var offset = IndexOf(gsw, marker, searchOffset);
            if (offset < 0)
            {
                return null;
            }

            var textureOffset = offset + TextureHeaderRelativeOffset;
            if (IsTextureSection(gsw, offset, textureOffset, out var length))
            {
                return new GswTextureSection(id, textureOffset, length);
            }

            searchOffset = offset + 1;
        }

        return null;
    }

    private static bool IsTextureSection(byte[] gsw, int sectionOffset, int textureOffset, out int length)
    {
        length = 0;
        if (!IsReadableRange(gsw, sectionOffset + IndexedC8MarkerRelativeOffset, 2)
            || BigEndian.ReadUInt16(gsw, sectionOffset + IndexedC8MarkerRelativeOffset) != 0x0801)
        {
            return false;
        }

        if (!IsReadableRange(gsw, sectionOffset + FormatRelativeOffset, 4)
            || !TextureFormats.Contains(BigEndian.ReadUInt32(gsw, sectionOffset + FormatRelativeOffset)))
        {
            return false;
        }

        if (!IsReadableRange(gsw, sectionOffset + PaletteStartRelativeOffset, 4))
        {
            return false;
        }

        length = checked((int)BigEndian.ReadUInt32(gsw, sectionOffset + PaletteStartRelativeOffset) + PaletteLength);
        return IsReadableRange(gsw, textureOffset, length);
    }

    private static int IndexOf(byte[] data, byte[] pattern, int start)
    {
        for (var offset = start; offset <= data.Length - pattern.Length; offset++)
        {
            if (data.AsSpan(offset, pattern.Length).SequenceEqual(pattern))
            {
                return offset;
            }
        }

        return -1;
    }

    private static bool IsReadableRange(byte[] bytes, int offset, int length)
        => offset >= 0 && length >= 0 && offset + length <= bytes.Length;

    private sealed record GswTextureSection(int Id, int TextureOffset, int Length);
}
