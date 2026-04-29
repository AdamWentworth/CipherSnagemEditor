using CipherSnagemEditor.Core.Binary;

namespace CipherSnagemEditor.Colosseum.Data;

public sealed record ColosseumCollisionData(
    IReadOnlyList<ColosseumCollisionTriangle> Triangles,
    IReadOnlyList<int> InteractableIndexes,
    IReadOnlyList<int> SectionIndexes)
{
    private const int EntrySize = 0x40;
    private const int FirstCollisionPointerOffset = 0x24;
    private const int FaceSize = 0x34;
    private const int VectorStride = 0x0c;

    public static ColosseumCollisionData Empty { get; } = new([], [], []);

    public static ColosseumCollisionData Parse(byte[] bytes)
    {
        if (bytes.Length < 8)
        {
            return Empty;
        }

        var data = new BinaryData(bytes);
        var listStart = checked((int)data.ReadUInt32(0));
        var entryCount = checked((int)data.ReadUInt32(4));
        if (listStart < 0 || listStart >= data.Length || entryCount <= 0)
        {
            return Empty;
        }

        var triangles = new List<ColosseumCollisionTriangle>();
        var interactableIndexes = new HashSet<int>();
        var sectionIndexes = new List<int>();
        var currentSectionIndex = -1;
        var maxDistance = 0f;

        for (var entry = 0; entry < entryCount; entry++)
        {
            var entryOffset = listStart + (entry * EntrySize);
            if (entryOffset < 0 || entryOffset + EntrySize > data.Length)
            {
                continue;
            }

            for (var pointerOffset = FirstCollisionPointerOffset; pointerOffset < EntrySize; pointerOffset += 4)
            {
                var collisionTableOffset = checked((int)data.ReadUInt32(entryOffset + pointerOffset));
                if (collisionTableOffset <= 0 || collisionTableOffset + 8 > data.Length)
                {
                    continue;
                }

                var isInteractable = pointerOffset is 0x2c or 0x30;
                var faceStart = checked((int)data.ReadUInt32(collisionTableOffset));
                var faceCount = checked((int)data.ReadUInt32(collisionTableOffset + 4));
                if (faceStart < 0 || faceStart >= data.Length || faceCount <= 0)
                {
                    continue;
                }

                currentSectionIndex++;
                sectionIndexes.Add(currentSectionIndex);

                for (var face = 0; face < faceCount; face++)
                {
                    var faceOffset = faceStart + (face * FaceSize);
                    if (faceOffset < 0 || faceOffset + FaceSize > data.Length)
                    {
                        continue;
                    }

                    var typeOffset = pointerOffset == 0x30 ? 0x32 : 0x30;
                    var indexOffset = pointerOffset == 0x30 ? 0x30 : 0x32;
                    var type = data.ReadUInt16(faceOffset + typeOffset);
                    var interactionIndex = data.ReadUInt16(faceOffset + indexOffset);
                    if (isInteractable)
                    {
                        interactableIndexes.Add(interactionIndex);
                    }

                    var normalX = ReadFloat(data, faceOffset + (3 * VectorStride));
                    var normalY = ReadFloat(data, faceOffset + (3 * VectorStride) + 4);
                    var normalZ = ReadFloat(data, faceOffset + (3 * VectorStride) + 8);
                    var vertices = new ColosseumCollisionVertex[3];
                    for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                    {
                        var x = ReadFloat(data, faceOffset + (vertexIndex * VectorStride));
                        var y = ReadFloat(data, faceOffset + (vertexIndex * VectorStride) + 4);
                        var z = ReadFloat(data, faceOffset + (vertexIndex * VectorStride) + 8);
                        maxDistance = Math.Max(maxDistance, Math.Abs(x));
                        maxDistance = Math.Max(maxDistance, Math.Abs(y));
                        maxDistance = Math.Max(maxDistance, Math.Abs(z));
                        vertices[vertexIndex] = new ColosseumCollisionVertex(
                            x,
                            y,
                            z,
                            normalX,
                            normalY,
                            normalZ,
                            type,
                            triangles.Count * 3 + vertexIndex,
                            isInteractable,
                            interactionIndex,
                            currentSectionIndex,
                            currentSectionIndex);
                    }

                    triangles.Add(new ColosseumCollisionTriangle(
                        vertices[0],
                        vertices[1],
                        vertices[2],
                        type,
                        isInteractable,
                        interactionIndex,
                        currentSectionIndex));
                }
            }
        }

        if (maxDistance > 0)
        {
            triangles = triangles
                .Select(triangle => ScaleTriangle(triangle, maxDistance))
                .ToList();
        }

        return new ColosseumCollisionData(
            triangles,
            interactableIndexes.OrderBy(index => index).ToArray(),
            sectionIndexes.Distinct().OrderBy(index => index).ToArray());
    }

    private static ColosseumCollisionTriangle ScaleTriangle(ColosseumCollisionTriangle triangle, float maxDistance)
        => triangle with
        {
            A = ScaleVertex(triangle.A, maxDistance),
            B = ScaleVertex(triangle.B, maxDistance),
            C = ScaleVertex(triangle.C, maxDistance)
        };

    private static ColosseumCollisionVertex ScaleVertex(ColosseumCollisionVertex vertex, float maxDistance)
        => vertex with
        {
            X = vertex.X / maxDistance,
            Y = vertex.Y / maxDistance,
            Z = vertex.Z / maxDistance
        };

    private static float ReadFloat(BinaryData data, int offset)
        => BitConverter.Int32BitsToSingle(unchecked((int)data.ReadUInt32(offset)));
}
