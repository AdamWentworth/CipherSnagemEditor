using System.Globalization;
using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Colosseum;

public enum ColosseumRawTableFieldKind
{
    UInt8,
    Int8,
    UInt16,
    Int16,
    UInt32,
    Int32,
    Float32,
    Bytes
}

public sealed record ColosseumRawTableField(
    string Name,
    int Offset,
    ColosseumRawTableFieldKind Kind,
    int Length = 0)
{
    public int ByteLength
        => Kind switch
        {
            ColosseumRawTableFieldKind.UInt8 or ColosseumRawTableFieldKind.Int8 => 1,
            ColosseumRawTableFieldKind.UInt16 or ColosseumRawTableFieldKind.Int16 => 2,
            ColosseumRawTableFieldKind.UInt32 or ColosseumRawTableFieldKind.Int32 or ColosseumRawTableFieldKind.Float32 => 4,
            ColosseumRawTableFieldKind.Bytes => Length,
            _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null)
        };
}

public sealed class ColosseumRawTableSchema
{
    private static readonly IReadOnlyDictionary<string, ColosseumRawTableField[]> SchemaFields =
        new Dictionary<string, ColosseumRawTableField[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["AI Weight Effect"] =
            [
                S32("Effect", 0x00),
                U32("Task Name ID", 0x04),
                U32("Sub Task Name ID", 0x08),
                U32("Reason Name ID", 0x0c),
                U32("Unknown", 0x10)
            ],
            ["Battle"] =
            [
                U8("Battle Type", 0x00),
                U8("Battle Style", 0x01),
                U8("Unknown Flag", 0x02),
                U16("Battle Field ID", 0x04),
                U32("Name ID", 0x08),
                U32("BGM ID", 0x0c),
                U32("Unknown 3", 0x10),
                U32("Colosseum Round", 0x14),
                U16("Player 1 Trainer ID", 0x18),
                U32("Player 1 Controller Index", 0x1c),
                U16("Player 2 Trainer ID", 0x20),
                U32("Player 2 Controller Index", 0x24),
                U16("Player 3 Trainer ID", 0x28),
                U32("Player 3 Controller Index", 0x2c),
                U16("Player 4 Trainer ID", 0x30),
                U32("Player 4 Controller Index", 0x34)
            ],
            ["Battle Styles"] =
            [
                U8("Trainers per side", 0x00),
                U8("Pokemon per trainer", 0x01),
                U8("Active pokemon per trainer", 0x02),
                U32("Name ID", 0x04)
            ],
            ["Battle Types"] =
            [
                U8("Revert Changes After Battle", 0x00),
                U8("Enable Gym Badge Boosts", 0x01),
                U8("Count Pokemon As Seen", 0x02),
                U8("Count Pokemon as Caught", 0x03),
                U8("Enable Trainer Items", 0x04),
                U8("Can Call Pokemon", 0x05),
                U8("Can Run", 0x06),
                U8("Can Draw", 0x07),
                U8("Pokemon Gain Exp", 0x08),
                U8("Awards Prize Money", 0x09),
                U8("Add prize money to pool", 0x0a),
                U8("Pay Day Flag", 0x0b),
                U8("Enable Friendship Gain", 0x0c),
                U8("Can Trigger Pokerus", 0x0d),
                U8("Enable Critical Hits", 0x0e),
                U8("Enable Rui AI Flag", 0x0f),
                U8("Enable Rui Shadow Pokemon Reveal", 0x10),
                U8("Enable Soul Dew Effect", 0x11),
                U8("Boost Exp Gain", 0x12),
                U8("Is Boss Battle", 0x13),
                U8("Can Steal Items", 0x14),
                U8("Enable Pickup", 0x15),
                U8("Enable Hyper Mode", 0x16),
                U8("Enable Full HUD layout", 0x17),
                U8("Display Opponent Dialog", 0x18),
                U8("Enable Battle Rules", 0x19),
                U32("Name ID", 0x1c)
            ],
            ["Battlefield"] =
            [
                U8("Unknown 1", 0x00),
                U16("Unknown 2", 0x02),
                U32("Name ID", 0x04),
                U32("Unknown 4", 0x08),
                U32("Unknown 5", 0x0c),
                U32("Unknown 6", 0x10),
                U16("Unknown 7", 0x14),
                U16("Unknown 8", 0x16)
            ],
            ["Character"] =
            [
                U16("ID", 0x00),
                U32("Name ID", 0x04)
            ],
            ["Character Model"] =
            [
                U8("Flags", 0x00),
                U8("Unknown 1", 0x01),
                U8("Unknown 2", 0x02),
                U8("Unknown 3", 0x03),
                S8("Unknown IDs 1", 0x04),
                S8("Unknown IDs 2", 0x05),
                S8("Unknown IDs 3", 0x06),
                S8("Unknown IDs 4", 0x07),
                S8("Unknown IDs 5", 0x08),
                S8("Unknown IDs 6", 0x09),
                U32(".dat File Identifier", 0x0c),
                F32("Walking Animation Frame Count", 0x10),
                F32("Running Animation Frame Count", 0x14),
                F32("Collision Radius", 0x18),
                F32("Neck Bone Left X Translation Limit", 0x1c),
                F32("Neck Bone Right X Translation Limit", 0x20),
                F32("Neck Bone Upward Y Translation Limit", 0x24),
                F32("Neck Bone Downward Y Translation Limit", 0x28)
            ],
            ["Door"] =
            [
                S8("Unknown Values 1", 0x00),
                S8("Unknown Values 2", 0x01),
                S8("Unknown Values 3", 0x02),
                S8("Unknown Values 4", 0x03),
                S8("Unknown Values 5", 0x04),
                S8("Unknown Values 6", 0x05),
                S8("Unknown Values 7", 0x06),
                S8("Unknown Values 8", 0x07),
                U8("Unknown 2", 0x08),
                S16("Door Index", 0x0a),
                U16("Room ID", 0x0c),
                U16("Unknown 3", 0x0e),
                U16("Unknown 4", 0x10),
                U32("Room Dat ID", 0x14)
            ],
            ["Interaction Point"] =
            [
                U8("Interaction Method", 0x00),
                U16("Room ID", 0x02),
                U32("Collision Region Index", 0x04),
                U16("Script Marker", 0x08),
                U16("Script Function Index", 0x0a),
                U32("Parameter 1", 0x0c),
                U32("Parameter 2", 0x10),
                U32("Parameter 3", 0x14),
                U32("Parameter 4", 0x18)
            ],
            ["Item"] =
            [
                U8("Pocket", 0x00),
                U8("Is Locked", 0x01),
                U8("Padding", 0x02),
                U8("Unknown Flag", 0x03),
                U8("Battle Item ID", 0x04),
                U16("Price", 0x06),
                U16("Coupon Price", 0x08),
                U16("Hold Item ID", 0x0a),
                U32("Padding 2", 0x0c),
                U32("Name ID", 0x10),
                U32("Description ID", 0x14),
                U32("Parameter", 0x18),
                U32("Field Function Pointer", 0x1c),
                U32("Battle Function Pointer", 0x20),
                S8("Happiness Effect 1", 0x24),
                S8("Happiness Effect 2", 0x25),
                S8("Happiness Effect 3", 0x26)
            ],
            ["Move"] =
            [
                S8("Priority", 0x00),
                U8("Base PP", 0x01),
                U8("Type", 0x02),
                U8("Targets", 0x03),
                U8("Base Accuracy", 0x04),
                U8("Effect Accuracy", 0x05),
                U8("Makes Contact", 0x06),
                U8("Can Be Protected Against", 0x07),
                U8("Can Be Reflected By Magic Coat", 0x08),
                U8("Can Be Stolen By Snatch", 0x09),
                U8("Can Be Copied By Mirror Move", 0x0a),
                U8("Gains Flinch Chance With King's Rock", 0x0b),
                U8("Callable by Metronome", 0x0c),
                U8("Copyable by Mimic", 0x0d),
                U8("Callable by Assist", 0x0e),
                U8("Callable by Sleep Talk", 0x0f),
                U8("Is Sound Based", 0x10),
                U8("Has unspecified target", 0x11),
                U8("Is HM Move", 0x12),
                U8("Has Risk Flag", 0x13),
                U8("Contest Appeal Jam Index", 0x14),
                U8("Contest Appeal Type", 0x15),
                U16("Base Power", 0x16),
                U32("Effect", 0x18),
                U16("Animation ID", 0x1c),
                U8("Unused", 0x1e),
                U8("Category", 0x1f),
                U32("Name ID", 0x20),
                U32("Unused String ID", 0x24),
                U32("Exclamation String ID", 0x28),
                U32("Description ID", 0x2c),
                U16("Padding 2", 0x30),
                U16("Animation ID 2", 0x32),
                U8("Move Effect Type", 0x34),
                U8("Secondary Effect Type", 0x35)
            ],
            ["Multiplier"] =
            [
                U8("Numerator", 0x00),
                U8("Denominator", 0x01)
            ],
            ["Nature"] =
            [
                U8("Battle Purification Multiplier", 0x00),
                U8("Walking Purification Multiplier", 0x01),
                U8("Call Purification Multiplier", 0x02),
                U8("Day Care Purification Multiplier", 0x03),
                U8("Cologne Massage Purification Multiplier", 0x04),
                U8("Attack Multiplier", 0x05),
                U8("Defense Multiplier", 0x06),
                U8("Sp.Atk Multiplier", 0x07),
                U8("Sp.Def Multiplier", 0x08),
                U8("Speed Multiplier", 0x09)
            ],
            ["Pokemon Stats"] = PokemonStatsFields(),
            ["Pokemon AI Roles"] =
            [
                U32("Name ID", 0x00),
                U32("Unknown 1", 0x04),
                U8("Move Type Weights No Effect", 0x08),
                U8("Move Type Weights Attack", 0x09),
                U8("Move Type Weights Healing", 0x0a),
                U8("Move Type Weights Stat Decrease", 0x0b),
                U8("Move Type Weights Stat Increase", 0x0c),
                U8("Move Type Weights Status", 0x0d),
                U8("Move Type Weights Field", 0x0e),
                U8("Move Type Weights Affect Opponent's Move", 0x0f),
                U8("Move Type Weights OHKO", 0x10),
                U8("Move Type Weights Multi-turn", 0x11),
                U8("Move Type Weights Misc", 0x12),
                U8("Move Type Weights Misc 2", 0x13)
            ],
            ["Pokeface"] =
            [
                U8("Regular or Shiny Image", 0x00),
                U32("Image File ID", 0x04)
            ],
            ["Room"] = RoomFields(),
            ["Shadow Pokemon Data"] =
            [
                U8("Catch Rate", 0x00),
                U16("Species", 0x02),
                U16("First Trainer Index", 0x04),
                U16("Alternative First Trainer Index", 0x06),
                U16("Heart Gauge", 0x08),
                U16("Index", 0x0a),
                U16("Unknown 1", 0x0c),
                U16("Padding", 0x0e),
                U16("Flag IDs Unknown 1", 0x10),
                U16("Flag IDs Captured Flag", 0x12),
                U16("Flag IDs Unknown 2", 0x14),
                U16("Flag IDs Unknown 3", 0x16),
                U32("Unknown 2", 0x18),
                U16("Unknown 3", 0x1c),
                U16("Unknown 4", 0x1e),
                U32("Debug Name ID", 0x20),
                U16("Unknown Values 2 1", 0x24),
                U16("Unknown Values 2 2", 0x26),
                U16("Unknown Values 2 3", 0x28),
                U16("Unknown Values 2 4", 0x2a),
                U16("Unknown Values 2 5", 0x2c),
                U16("Unknown Values 2 6", 0x2e),
                U32("Unknown Values 3 1", 0x30),
                U32("Unknown Values 3 2", 0x34)
            ],
            ["Sounds"] =
            [
                U32("Samp File ID", 0x00),
                U32("Unknown", 0x04)
            ],
            ["TM Or HM"] =
            [
                U8("Is HM", 0x00),
                U32("Move", 0x04)
            ],
            ["Trainer AI"] =
            [
                U8("Unknown Flags A 1", 0x00),
                U8("Unknown Flags A 2", 0x01),
                U8("Unknown Flags A 3", 0x02),
                U8("Unknown Flags A 4", 0x03),
                U8("Unknown Flags A 5", 0x04),
                U8("Unknown Flags A 6", 0x05),
                U8("Unknown Percentages A 1", 0x06),
                U8("Unknown Percentages A 2", 0x07),
                U8("Unknown Percentages A 3", 0x08),
                U8("Unknown Flags B 1", 0x09),
                U8("Unknown Flags B 2", 0x0a),
                U8("Unknown Flags B 3", 0x0b),
                U8("Unknown Flags B 4", 0x0c),
                U8("Unknown Percentages B", 0x0d),
                U8("Unknown Flag C", 0x0e),
                S8("Unknown Values A 1", 0x0f),
                S8("Unknown Values A 2", 0x10),
                U8("Unknown Flags D 1", 0x11),
                U8("Unknown Flags D 2", 0x12),
                U8("Unknown Flags D 3", 0x13),
                U8("Unknown Flags D 4", 0x14),
                U8("Unknown Flags D 5", 0x15),
                U8("Unknown Percentage C", 0x16),
                U8("Unknown Flag E", 0x17),
                U8("Unknown Percentage D", 0x18),
                U8("Unknown Flags F", 0x19),
                S32("Unknown ID", 0x1c),
                S8("Unknown Values B 1", 0x20),
                S8("Unknown Values B 2", 0x21),
                S8("Unknown Values B 3", 0x22),
                S8("Unknown Values B 4", 0x23),
                S8("Unknown Values B 5", 0x24),
                S8("Unknown Values B 6", 0x25),
                S8("Unknown Values B 7", 0x26),
                S8("Unknown Values B 8", 0x27)
            ],
            ["Trainer"] =
            [
                U8("Gender", 0x00),
                U16("Trainer Class", 0x02),
                U16("First Pokemon Index", 0x04),
                U16("AI Index", 0x06),
                U32("Name ID", 0x08),
                U32("Battle Transition", 0x0c),
                U32("Model ID", 0x10),
                U16("Item 1", 0x14),
                U16("Item 2", 0x16),
                U16("Item 3", 0x18),
                U16("Item 4", 0x1a),
                U16("Item 5", 0x1c),
                U16("Item 6", 0x1e),
                U16("Item 7", 0x20),
                U16("Item 8", 0x22),
                U32("Pre Battle Text Id", 0x24),
                U32("Victory Text Id", 0x28),
                U32("Loss Text Id", 0x2c),
                U32("Padding", 0x30)
            ],
            ["Trainer Class"] =
            [
                U16("Payout", 0x00),
                U32("Name ID", 0x04),
                U32("Unknown", 0x08)
            ],
            ["Trainer Pokemon"] =
            [
                S8("Ability Slot", 0x00),
                U8("Gender", 0x01),
                U8("Nature", 0x02),
                U8("Shadow ID", 0x03),
                U8("Level", 0x04),
                U8("Unknown 1", 0x05),
                U8("AI Role", 0x06),
                S16("Happiness", 0x08),
                U16("Species", 0x0a),
                U16("Pokeball", 0x0c),
                U16("Padding", 0x0e),
                U32("Held Item", 0x10),
                U32("Name ID", 0x14),
                U32("Unknown 5", 0x18),
                S8("IVs HP", 0x1c),
                S8("IVs Attack", 0x1d),
                S8("IVs Defense", 0x1e),
                S8("IVs Sp.Atk", 0x1f),
                S8("IVs Sp.Def", 0x20),
                S8("IVs Speed", 0x21),
                S16("EVs HP", 0x22),
                S16("EVs Attack", 0x24),
                S16("EVs Defense", 0x26),
                S16("EVs Sp.Atk", 0x28),
                S16("EVs Sp.Def", 0x2a),
                S16("EVs Speed", 0x2c),
                U16("Padding 2", 0x2e),
                U32("Move 1 Padding", 0x30),
                S16("Move 1 PP", 0x34),
                S16("Move 1", 0x36),
                U32("Move 2 Padding", 0x38),
                S16("Move 2 PP", 0x3c),
                S16("Move 2", 0x3e),
                U32("Move 3 Padding", 0x40),
                S16("Move 3 PP", 0x44),
                S16("Move 3", 0x46),
                U32("Move 4 Padding", 0x48),
                S16("Move 4 PP", 0x4c),
                S16("Move 4", 0x4e)
            ],
            ["Treasure"] =
            [
                U8("Model ID", 0x00),
                U8("Quantity", 0x01),
                U16("Angle", 0x02),
                U16("Room ID", 0x04),
                U16("Flag ID", 0x06),
                U16("Unknown", 0x08),
                U16("Padding", 0x0a),
                U32("Item ID", 0x0c),
                F32("Coordinates Position X", 0x10),
                F32("Coordinates Position Y", 0x14),
                F32("Coordinates Position Z", 0x18)
            ],
            ["Type"] = TypeFields(),
            ["Valid Item"] =
            [
                U16("Item ID", 0x00)
            ],
            ["Valid Item 2"] =
            [
                U16("Item ID", 0x00)
            ]
        };

    public ColosseumRawTableSchema(string name, IReadOnlyList<ColosseumRawTableField> fields)
    {
        Name = name;
        Fields = fields;
    }

    public string Name { get; }

    public IReadOnlyList<ColosseumRawTableField> Fields { get; }

    public static ColosseumRawTableSchema? For(ColosseumRawTableDefinition definition, int entryLength)
        => For(definition.Name, entryLength);

    public static ColosseumRawTableSchema? For(string tableName, int entryLength)
    {
        if (!SchemaFields.TryGetValue(tableName, out var fields))
        {
            return null;
        }

        var usableFields = fields
            .Where(field => field.Offset >= 0 && field.Offset + field.ByteLength <= entryLength)
            .ToArray();
        return usableFields.Length == 0 ? null : new ColosseumRawTableSchema(tableName, usableFields);
    }

    public Dictionary<string, string> ReadFields(ReadOnlySpan<byte> row)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in Fields)
        {
            values[field.Name] = ReadField(row, field);
        }

        return values;
    }

    public void ApplyFields(Span<byte> row, IReadOnlyDictionary<string, string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return;
        }

        foreach (var field in Fields)
        {
            if (!values.TryGetValue(field.Name, out var value) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            WriteField(row, field, value);
        }
    }

    private static string ReadField(ReadOnlySpan<byte> row, ColosseumRawTableField field)
        => field.Kind switch
        {
            ColosseumRawTableFieldKind.UInt8 => row[field.Offset].ToString(CultureInfo.InvariantCulture),
            ColosseumRawTableFieldKind.Int8 => unchecked((sbyte)row[field.Offset]).ToString(CultureInfo.InvariantCulture),
            ColosseumRawTableFieldKind.UInt16 => BigEndian.ReadUInt16(row, field.Offset).ToString(CultureInfo.InvariantCulture),
            ColosseumRawTableFieldKind.Int16 => BigEndian.ReadInt16(row, field.Offset).ToString(CultureInfo.InvariantCulture),
            ColosseumRawTableFieldKind.UInt32 => BigEndian.ReadUInt32(row, field.Offset).ToString(CultureInfo.InvariantCulture),
            ColosseumRawTableFieldKind.Int32 => BigEndian.ReadInt32(row, field.Offset).ToString(CultureInfo.InvariantCulture),
            ColosseumRawTableFieldKind.Float32 => ReadSingle(row, field.Offset).ToString("R", CultureInfo.InvariantCulture),
            ColosseumRawTableFieldKind.Bytes => Convert.ToHexString(row.Slice(field.Offset, field.ByteLength)),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field.Kind, null)
        };

    private static void WriteField(Span<byte> row, ColosseumRawTableField field, string value)
    {
        switch (field.Kind)
        {
            case ColosseumRawTableFieldKind.UInt8:
                row[field.Offset] = checked((byte)ParseUnsigned(value, byte.MaxValue));
                break;
            case ColosseumRawTableFieldKind.Int8:
                row[field.Offset] = unchecked((byte)checked((sbyte)ParseSigned(value, sbyte.MinValue, sbyte.MaxValue)));
                break;
            case ColosseumRawTableFieldKind.UInt16:
                BigEndian.WriteUInt16(row, field.Offset, checked((ushort)ParseUnsigned(value, ushort.MaxValue)));
                break;
            case ColosseumRawTableFieldKind.Int16:
                BigEndian.WriteUInt16(row, field.Offset, unchecked((ushort)checked((short)ParseSigned(value, short.MinValue, short.MaxValue))));
                break;
            case ColosseumRawTableFieldKind.UInt32:
                BigEndian.WriteUInt32(row, field.Offset, checked((uint)ParseUnsigned(value, uint.MaxValue)));
                break;
            case ColosseumRawTableFieldKind.Int32:
                BigEndian.WriteUInt32(row, field.Offset, unchecked((uint)checked((int)ParseSigned(value, int.MinValue, int.MaxValue))));
                break;
            case ColosseumRawTableFieldKind.Float32:
                BigEndian.WriteUInt32(row, field.Offset, unchecked((uint)BitConverter.SingleToInt32Bits(ParseSingle(value))));
                break;
            case ColosseumRawTableFieldKind.Bytes:
                var bytes = ParseHex(value);
                if (bytes.Length != field.ByteLength)
                {
                    throw new InvalidDataException($"{field.Name} has {bytes.Length} bytes; expected {field.ByteLength}.");
                }

                bytes.CopyTo(row.Slice(field.Offset, field.ByteLength));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field), field.Kind, null);
        }
    }

    private static uint ParseUnsigned(string value, uint max)
    {
        var trimmed = value.Trim();
        if (bool.TryParse(trimmed, out var boolean))
        {
            return boolean ? 1u : 0u;
        }

        if (trimmed.StartsWith("-", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsigned table field cannot be negative: {value}");
        }

        var parsed = ParseInteger(trimmed);
        if (parsed < 0 || parsed > max)
        {
            throw new InvalidDataException($"Unsigned table field {value} is outside 0..{max}.");
        }

        return checked((uint)parsed);
    }

    private static long ParseSigned(string value, long min, long max)
    {
        var parsed = ParseInteger(value.Trim());
        if (parsed < min || parsed > max)
        {
            throw new InvalidDataException($"Signed table field {value} is outside {min}..{max}.");
        }

        return parsed;
    }

    private static long ParseInteger(string value)
    {
        var negative = value.StartsWith("-", StringComparison.Ordinal);
        var text = negative ? value[1..] : value;
        long parsed;
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            parsed = Convert.ToInt64(text[2..], 16);
        }
        else
        {
            parsed = long.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        return negative ? -parsed : parsed;
    }

    private static float ParseSingle(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            var bits = Convert.ToUInt32(trimmed[2..], 16);
            return BitConverter.Int32BitsToSingle(unchecked((int)bits));
        }

        return float.Parse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static float ReadSingle(ReadOnlySpan<byte> row, int offset)
        => BitConverter.Int32BitsToSingle(unchecked((int)BigEndian.ReadUInt32(row, offset)));

    private static byte[] ParseHex(string hex)
    {
        var compact = new string(hex.Where(Uri.IsHexDigit).ToArray());
        if (compact.Length % 2 != 0)
        {
            throw new InvalidDataException("Hex byte data has an odd number of digits.");
        }

        return Convert.FromHexString(compact);
    }

    private static ColosseumRawTableField[] PokemonStatsFields()
    {
        var fields = new List<ColosseumRawTableField>
        {
            U8("Level up Rate", 0x00),
            U8("Catch Rate", 0x01),
            U8("Gender Ratio", 0x02),
            U8("Unknown", 0x03),
            U8("Unknown Value", 0x04),
            U16("Exp yield", 0x06),
            U16("Base Happiness", 0x08),
            S16("Height", 0x0a),
            S16("Weight", 0x0c),
            U16("Cry ID", 0x0e),
            U16("National Dex Index", 0x10),
            U16("Shout ID", 0x12),
            U16("Hoenn Regional Dex ID", 0x14),
            U16("Unknown 4", 0x16),
            U32("Name ID", 0x18),
            U32("Species Name ID", 0x1c),
            U32("Unknown 5", 0x20),
            U32("Unknown 6", 0x24),
            U32("Capture Flag", 0x28),
            U32("Model ID", 0x2c),
            U8("Types 1", 0x30),
            U8("Types 2", 0x31),
            U8("Abilities 1", 0x32),
            U8("Abilities 2", 0x33)
        };

        for (var index = 0; index < 50; index++)
        {
            fields.Add(U8($"TM{index + 1:00}", 0x34 + index));
        }

        for (var index = 0; index < 8; index++)
        {
            fields.Add(U8($"HM{index + 1:00}", 0x66 + index));
        }

        fields.AddRange(
        [
            U8("Egg Groups 1", 0x6e),
            U8("Egg Groups 2", 0x6f),
            U16("Wild Items 1", 0x70),
            U16("Wild Items 2", 0x72)
        ]);

        for (var index = 0; index < 8; index++)
        {
            fields.Add(U16($"Egg Moves {index + 1}", 0x74 + (index * 2)));
        }

        AddStats(fields, "Base Stats", 0x84, signed: true);
        AddStats(fields, "EV Yields", 0x90, signed: true);

        for (var index = 0; index < 5; index++)
        {
            var offset = 0x9c + (index * 0x06);
            fields.Add(U8($"Evolutions {index + 1} Evolution Method", offset));
            fields.Add(U16($"Evolutions {index + 1} Evolution Condition", offset + 0x02));
            fields.Add(U16($"Evolutions {index + 1} Evolved Form", offset + 0x04));
        }

        for (var index = 0; index < 19; index++)
        {
            var offset = 0xba + (index * 0x04);
            fields.Add(U8($"Level Up Moves {index + 1} Level", offset));
            fields.Add(U16($"Level Up Moves {index + 1} Move", offset + 0x02));
        }

        fields.Add(U32("Padding", 0x108));
        fields.Add(U8("Regular Sprites Pokedex Colour ID", 0x10c));
        fields.Add(U16("Regular Sprites Face ID", 0x10e));
        fields.Add(U32("Regular Sprites Body Image ID", 0x110));
        fields.Add(U8("Shiny Sprites Pokedex Colour ID", 0x114));
        fields.Add(U16("Shiny Sprites Face ID", 0x116));
        fields.Add(U32("Shiny Sprites Body Image ID", 0x118));
        return fields.ToArray();
    }

    private static ColosseumRawTableField[] RoomFields()
    {
        var fields = new List<ColosseumRawTableField>
        {
            U8("Unknown Flags", 0x00),
            U8("Area ID", 0x01),
            U32("FsysID", 0x04),
            U32("Padding", 0x08),
            U32("Room ID", 0x0c)
        };

        for (var index = 0; index < 5; index++)
        {
            fields.Add(U32($"Unused Values {index + 1}", 0x10 + (index * 4)));
        }

        fields.Add(U32("Name ID", 0x24));
        for (var index = 0; index < 5; index++)
        {
            var offset = 0x28 + (index * 4);
            fields.Add(U16($"Unknown Script Functions {index + 1} Script", offset));
            fields.Add(U16($"Unknown Script Functions {index + 1} Function Index", offset + 2));
        }

        for (var index = 0; index < 4; index++)
        {
            fields.Add(U32($"Unused Values 2 {index + 1}", 0x3c + (index * 4)));
        }

        for (var index = 0; index < 5; index++)
        {
            fields.Add(U32($"Fsys IDs {index + 1}", 0x4c + (index * 4)));
        }

        return fields.ToArray();
    }

    private static ColosseumRawTableField[] TypeFields()
    {
        var fields = new List<ColosseumRawTableField>
        {
            U8("Category", 0x00),
            U16("Icon Image ID", 0x02),
            U32("Name ID", 0x04)
        };

        var typeNames = new[]
        {
            "Normal",
            "Fight",
            "Flying",
            "Poison",
            "Ground",
            "Rock",
            "Bug",
            "Ghost",
            "Steel",
            "Fairy",
            "Fire",
            "Water",
            "Grass",
            "Electr",
            "Psychc",
            "Ice",
            "Dragon",
            "Dark"
        };
        for (var index = 0; index < typeNames.Length; index++)
        {
            fields.Add(U16($"Effectiveness against {typeNames[index]}", 0x08 + (index * 2)));
        }

        return fields.ToArray();
    }

    private static void AddStats(List<ColosseumRawTableField> fields, string prefix, int start, bool signed)
    {
        var names = new[] { "HP", "Attack", "Defense", "Sp.Atk", "Sp.Def", "Speed" };
        for (var index = 0; index < names.Length; index++)
        {
            fields.Add(signed ? S16($"{prefix} {names[index]}", start + (index * 2)) : U16($"{prefix} {names[index]}", start + (index * 2)));
        }
    }

    private static ColosseumRawTableField U8(string name, int offset)
        => new(name, offset, ColosseumRawTableFieldKind.UInt8);

    private static ColosseumRawTableField S8(string name, int offset)
        => new(name, offset, ColosseumRawTableFieldKind.Int8);

    private static ColosseumRawTableField U16(string name, int offset)
        => new(name, offset, ColosseumRawTableFieldKind.UInt16);

    private static ColosseumRawTableField S16(string name, int offset)
        => new(name, offset, ColosseumRawTableFieldKind.Int16);

    private static ColosseumRawTableField U32(string name, int offset)
        => new(name, offset, ColosseumRawTableFieldKind.UInt32);

    private static ColosseumRawTableField S32(string name, int offset)
        => new(name, offset, ColosseumRawTableFieldKind.Int32);

    private static ColosseumRawTableField F32(string name, int offset)
        => new(name, offset, ColosseumRawTableFieldKind.Float32);
}
