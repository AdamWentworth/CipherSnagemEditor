using CipherSnagemEditor.Colosseum;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Tests;

public sealed class ColosseumRawTableSchemaTests
{
    [Fact]
    public void MoveSchemaReadsSwiftStyleFieldNames()
    {
        var schema = ColosseumRawTableSchema.For("Move", 0x38);
        Assert.NotNull(schema);
        var row = new byte[0x38];
        row[0x00] = 0xff;
        row[0x01] = 15;
        BigEndian.WriteUInt16(row, 0x16, 75);
        BigEndian.WriteUInt32(row, 0x18, 5);
        BigEndian.WriteUInt16(row, 0x1c, 8);
        BigEndian.WriteUInt32(row, 0x20, 2008);

        var fields = schema!.ReadFields(row);

        Assert.Equal("-1", fields["Priority"]);
        Assert.Equal("15", fields["Base PP"]);
        Assert.Equal("75", fields["Base Power"]);
        Assert.Equal("5", fields["Effect"]);
        Assert.Equal("8", fields["Animation ID"]);
        Assert.Equal("2008", fields["Name ID"]);
    }

    [Fact]
    public void MoveSchemaAppliesEditedFieldsOverRawBytes()
    {
        var schema = ColosseumRawTableSchema.For("Move", 0x38);
        Assert.NotNull(schema);
        var row = new byte[0x38];

        schema!.ApplyFields(row, new Dictionary<string, string>
        {
            ["Priority"] = "-2",
            ["Base PP"] = "20",
            ["Base Power"] = "100",
            ["Effect"] = "0x1234",
            ["Is HM Move"] = "true",
            ["Move Effect Type"] = "5"
        });

        Assert.Equal(0xfe, row[0x00]);
        Assert.Equal(20, row[0x01]);
        Assert.Equal(100, BigEndian.ReadUInt16(row, 0x16));
        Assert.Equal(0x1234u, BigEndian.ReadUInt32(row, 0x18));
        Assert.Equal(1, row[0x12]);
        Assert.Equal(5, row[0x34]);
    }

    [Fact]
    public void TreasureSchemaRoundTripsFloats()
    {
        var schema = ColosseumRawTableSchema.For("Treasure", 0x1c);
        Assert.NotNull(schema);
        var row = new byte[0x1c];

        schema!.ApplyFields(row, new Dictionary<string, string>
        {
            ["Model ID"] = "0x44",
            ["Quantity"] = "3",
            ["Coordinates Position X"] = "-94",
            ["Coordinates Position Y"] = "8.5",
            ["Coordinates Position Z"] = "44"
        });
        var fields = schema.ReadFields(row);

        Assert.Equal("68", fields["Model ID"]);
        Assert.Equal("3", fields["Quantity"]);
        Assert.Equal("-94", fields["Coordinates Position X"]);
        Assert.Equal("8.5", fields["Coordinates Position Y"]);
        Assert.Equal("44", fields["Coordinates Position Z"]);
    }
}
