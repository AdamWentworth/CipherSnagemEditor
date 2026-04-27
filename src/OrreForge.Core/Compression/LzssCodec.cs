using OrreForge.Core.Binary;

namespace OrreForge.Core.Compression;

public static class LzssCodec
{
    public const uint Magic = 0x4c5a5353;
    private const int Ei = 12;
    private const int Ej = 4;
    private const int P = 2;
    private const int HeaderSize = 0x10;

    public static bool HasHeader(ReadOnlySpan<byte> data)
        => data.Length >= 4 && BigEndian.ReadUInt32(data, 0) == Magic;

    public static byte[] DecodeFile(ReadOnlySpan<byte> data)
    {
        if (!HasHeader(data))
        {
            return DecodePayload(data);
        }

        return DecodePayload(data[HeaderSize..]);
    }

    public static byte[] DecodePayload(ReadOnlySpan<byte> payload)
    {
        var inputBytes = payload.ToArray();
        var n = 1 << Ei;
        var f = 1 << Ej;
        const int rless = 2;

        var slidingWindow = new byte[n];
        var output = new List<byte>();
        var inputPosition = 0;

        var r = (n - f) - rless;
        uint flags = 0;

        n -= 1;
        f -= 1;

        byte Read()
        {
            if (inputPosition >= inputBytes.Length)
            {
                throw new InvalidDataException("LZSS stream ended unexpectedly.");
            }

            return inputBytes[inputPosition++];
        }

        while (inputPosition < inputBytes.Length)
        {
            if ((flags & 0x100) == 0)
            {
                flags = Read();
                flags |= 0xff00;
            }

            if ((flags & 1) != 0)
            {
                var c = Read();
                output.Add(c);
                slidingWindow[r] = c;
                r = (r + 1) & n;
            }
            else
            {
                var i = Read();
                var j = Read();

                var position = i | ((j >> Ej) << 8);
                var count = (j & f) + P;
                for (var k = 0; k <= count; k++)
                {
                    var c = slidingWindow[(position + k) & n];
                    output.Add(c);
                    slidingWindow[r] = c;
                    r = (r + 1) & n;
                }
            }

            flags >>= 1;
        }

        return output.ToArray();
    }
}
