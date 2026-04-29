using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Core.Compression;

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

    public static byte[] EncodeFile(ReadOnlySpan<byte> data)
    {
        var payload = EncodePayload(data);
        var output = new byte[HeaderSize + payload.Length];
        BigEndian.WriteUInt32(output, 0, Magic);
        BigEndian.WriteUInt32(output, 4, checked((uint)data.Length));
        BigEndian.WriteUInt32(output, 8, checked((uint)output.Length));
        BigEndian.WriteUInt32(output, 12, 0);
        payload.CopyTo(output.AsSpan(HeaderSize));
        return output;
    }

    public static byte[] EncodePayload(ReadOnlySpan<byte> data)
    {
        const int threshold = 2;
        var n = 1 << Ei;
        var f = (1 << Ej) + threshold;
        var nil = n;

        var inputBytes = data.ToArray();
        if (inputBytes.Length == 0)
        {
            return [];
        }

        var textBuffer = new byte[n + f - 1];
        var leftChildren = new int[n + 1];
        var rightChildren = new int[n + 257];
        var parents = new int[n + 1];

        for (var i = n + 1; i <= n + 256; i++)
        {
            rightChildren[i] = nil;
        }

        for (var i = 0; i < n; i++)
        {
            parents[i] = nil;
        }

        var inputPosition = 0;
        var output = new List<byte>();
        var matchPosition = 0;
        var matchLength = 0;

        byte Read() => inputBytes[inputPosition++];

        void Insert(int node)
        {
            var cmp = 1;
            var parent = n + 1 + textBuffer[node];
            rightChildren[node] = nil;
            leftChildren[node] = nil;
            matchLength = 0;

            while (true)
            {
                if (cmp >= 0)
                {
                    if (rightChildren[parent] != nil)
                    {
                        parent = rightChildren[parent];
                    }
                    else
                    {
                        rightChildren[parent] = node;
                        parents[node] = parent;
                        return;
                    }
                }
                else
                {
                    if (leftChildren[parent] != nil)
                    {
                        parent = leftChildren[parent];
                    }
                    else
                    {
                        leftChildren[parent] = node;
                        parents[node] = parent;
                        return;
                    }
                }

                var i = 1;
                while (i < f)
                {
                    cmp = textBuffer[node + i] - textBuffer[parent + i];
                    if (cmp != 0)
                    {
                        break;
                    }

                    i++;
                }

                if (i > matchLength)
                {
                    matchPosition = parent;
                    matchLength = i;
                    if (matchLength >= f)
                    {
                        break;
                    }
                }
            }

            parents[node] = parents[parent];
            leftChildren[node] = leftChildren[parent];
            rightChildren[node] = rightChildren[parent];
            parents[leftChildren[parent]] = node;
            parents[rightChildren[parent]] = node;

            if (rightChildren[parents[parent]] == parent)
            {
                rightChildren[parents[parent]] = node;
            }
            else
            {
                leftChildren[parents[parent]] = node;
            }

            parents[parent] = nil;
        }

        void Delete(int node)
        {
            if (parents[node] == nil)
            {
                return;
            }

            int newChild;
            if (rightChildren[node] == nil)
            {
                newChild = leftChildren[node];
            }
            else if (leftChildren[node] == nil)
            {
                newChild = rightChildren[node];
            }
            else
            {
                newChild = leftChildren[node];
                if (rightChildren[newChild] != nil)
                {
                    do
                    {
                        newChild = rightChildren[newChild];
                    }
                    while (rightChildren[newChild] != nil);

                    rightChildren[parents[newChild]] = leftChildren[newChild];
                    parents[leftChildren[newChild]] = parents[newChild];
                    leftChildren[newChild] = leftChildren[node];
                    parents[leftChildren[node]] = newChild;
                }

                rightChildren[newChild] = rightChildren[node];
                parents[rightChildren[node]] = newChild;
            }

            parents[newChild] = parents[node];

            if (rightChildren[parents[node]] == node)
            {
                rightChildren[parents[node]] = newChild;
            }
            else
            {
                leftChildren[parents[node]] = newChild;
            }

            parents[node] = nil;
        }

        var codeBuffer = new List<byte> { 0 };
        byte mask = 1;
        var length = Math.Min(f, inputBytes.Length);
        var r = n - f;
        var s = 0;

        for (var i = 0; i < length; i++)
        {
            textBuffer[r + i] = Read();
        }

        for (var i = 1; i <= f; i++)
        {
            Insert(r - i);
        }

        Insert(r);

        do
        {
            if (matchLength > length)
            {
                matchLength = length;
            }

            if (matchLength <= threshold)
            {
                matchLength = 1;
                codeBuffer[0] |= mask;
                codeBuffer.Add(textBuffer[r]);
            }
            else
            {
                codeBuffer.Add((byte)(matchPosition & 0xff));
                codeBuffer.Add((byte)(((matchPosition >> 4) & 0xf0) | (matchLength - (threshold + 1))));
            }

            if (mask == 0x80)
            {
                output.AddRange(codeBuffer);
                codeBuffer = [0];
                mask = 1;
            }
            else
            {
                mask <<= 1;
            }

            var lastMatchLength = matchLength;
            var consumed = 0;
            for (; consumed < lastMatchLength && inputPosition < inputBytes.Length; consumed++)
            {
                var c = Read();
                Delete(s);
                textBuffer[s] = c;
                if (s < f - 1)
                {
                    textBuffer[s + n] = c;
                }

                s = (s + 1) % n;
                r = (r + 1) % n;
                Insert(r);
            }

            while (consumed < lastMatchLength)
            {
                Delete(s);
                s = (s + 1) % n;
                r = (r + 1) % n;
                length--;
                if (length != 0)
                {
                    Insert(r);
                }

                consumed++;
            }
        }
        while (length > 0);

        if (codeBuffer.Count > 1)
        {
            output.AddRange(codeBuffer);
        }

        return output.ToArray();
    }
}
