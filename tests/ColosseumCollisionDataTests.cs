using CipherSnagemEditor.Colosseum.Data;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Tests;

public sealed class ColosseumCollisionDataTests
{
    [Fact]
    public void ParsesInteractableCollisionTrianglesUsingSwiftLayout()
    {
        var bytes = BuildCollisionData();

        var collision = ColosseumCollisionData.Parse(bytes);

        var triangle = Assert.Single(collision.Triangles);
        Assert.True(triangle.IsInteractable);
        Assert.Equal(5, triangle.Type);
        Assert.Equal(42, triangle.InteractionIndex);
        Assert.Equal(0, triangle.SectionIndex);
        Assert.Equal([42], collision.InteractableIndexes);
        Assert.Equal([0], collision.SectionIndexes);

        Assert.Equal(0f, triangle.A.X);
        Assert.Equal(0f, triangle.A.Y);
        Assert.Equal(0f, triangle.A.Z);
        Assert.Equal(1f, triangle.B.X);
        Assert.Equal(0f, triangle.B.Y);
        Assert.Equal(0f, triangle.B.Z);
        Assert.Equal(0f, triangle.C.X);
        Assert.Equal(0.5f, triangle.C.Y);
        Assert.Equal(0f, triangle.C.Z);

        Assert.Equal(0f, triangle.A.NormalX);
        Assert.Equal(1f, triangle.A.NormalY);
        Assert.Equal(0f, triangle.A.NormalZ);
    }

    [Fact]
    public void IgnoresInvalidCollisionHeaders()
    {
        var collision = ColosseumCollisionData.Parse([0, 0, 0, 0, 0, 0, 0, 1]);

        Assert.Empty(collision.Triangles);
        Assert.Empty(collision.InteractableIndexes);
        Assert.Empty(collision.SectionIndexes);
    }

    private static byte[] BuildCollisionData()
    {
        var bytes = new byte[0x140];
        WriteUInt32(bytes, 0x00, 0x20);
        WriteUInt32(bytes, 0x04, 1);

        WriteUInt32(bytes, 0x20 + 0x2c, 0x80);
        WriteUInt32(bytes, 0x80, 0x100);
        WriteUInt32(bytes, 0x84, 1);

        WriteFloat(bytes, 0x100, 0f);
        WriteFloat(bytes, 0x104, 0f);
        WriteFloat(bytes, 0x108, 0f);
        WriteFloat(bytes, 0x10c, 4f);
        WriteFloat(bytes, 0x110, 0f);
        WriteFloat(bytes, 0x114, 0f);
        WriteFloat(bytes, 0x118, 0f);
        WriteFloat(bytes, 0x11c, 2f);
        WriteFloat(bytes, 0x120, 0f);
        WriteFloat(bytes, 0x124, 0f);
        WriteFloat(bytes, 0x128, 1f);
        WriteFloat(bytes, 0x12c, 0f);
        BigEndian.WriteUInt16(bytes, 0x130, 5);
        BigEndian.WriteUInt16(bytes, 0x132, 42);

        return bytes;
    }

    private static void WriteUInt32(byte[] bytes, int offset, int value)
        => BigEndian.WriteUInt32(bytes, offset, unchecked((uint)value));

    private static void WriteFloat(byte[] bytes, int offset, float value)
        => BigEndian.WriteUInt32(bytes, offset, unchecked((uint)BitConverter.SingleToInt32Bits(value)));
}
