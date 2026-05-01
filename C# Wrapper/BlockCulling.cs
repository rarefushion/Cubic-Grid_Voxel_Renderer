using System.Numerics;
using GalensUnified.CubicGrid.Core;

namespace GalensUnified.CubicGrid.Renderer.NET;

public static class BlockCulling
{
    /// <summary>Provides a standared order for directions. -z, +z, +y, -y, -x then +x.</summary>
    public static readonly Vector3[] directions =
    [
        -Vector3.UnitZ,
         Vector3.UnitZ,
         Vector3.UnitY,
        -Vector3.UnitY,
        -Vector3.UnitX,
         Vector3.UnitX
    ];

    /// <summary>Calculates the 1D index of a local chunk position with z > y > x index ordering.</summary>
    public static int IndexByLocalPos(Vector3 position, int chunkLength) =>
        ((int)position.Z * chunkLength + (int)position.Y) * chunkLength + (int)position.X;

    /// <summary>Determines if <paramref name="position"/> is within the chunk.</summary>
    public static bool IsLocal(Vector3 position, int chunkLength) =>
        position.X >= 0 && position.X < chunkLength &&
        position.Y >= 0 && position.Y < chunkLength &&
        position.Z >= 0 && position.Z < chunkLength;

    /// <summary>
    /// Determines if the block at <paramref name="position"/> is air.
    /// Uses <see cref="IndexByLocalPos"/> which assumes  z > y > x index ordering.
    /// If <paramref name="position"/> is outside the chunk it will assume air and return true.
    /// </summary>
    public static bool IsAir(Vector3 position, Span<ushort> blocks, int chunkLength) =>
        !IsLocal(position, chunkLength) || blocks[IndexByLocalPos(position, chunkLength)] == 0;

    /// <summary>Wraps a global position into a local chunk position.</summary>
    /// <remarks>
    /// Uses a double-masking pattern to correctly handle negative coordinates,
    /// ensuring the result is always a positive offset within the region.
    /// </remarks>
    public static Vector3 LocalPosByGlobalPos(Vector3 pos, int chunkLength) => new
    (
        ((pos.X % chunkLength) + chunkLength) % chunkLength,
        ((pos.Y % chunkLength) + chunkLength) % chunkLength,
        ((pos.Z % chunkLength) + chunkLength) % chunkLength
    );

    /// <summary>Culls any blocks that isn't touching air. Chunk borders are assumed air. face lights default to 1.</summary>
    /// <param name="blocks">The collection of block IDs comprising the chunk. With z > y > x index ordering.</param>
    public static FaceInstance[] CullSingleChunk(Span<ushort> blocks, int chunkLength)
    {
        List<FaceInstance> instances = [];
        for (int z = 0; z < chunkLength; z++)
        for (int y = 0; y < chunkLength; y++)
        for (int x = 0; x < chunkLength; x++)
        {
            Vector3 pos = new(x, y, z);
            if (IsAir(pos, blocks, chunkLength))
                continue;
            for (int d = 0; d < 6; d++)
                if (IsAir(pos + directions[d], blocks, chunkLength))
                    instances.Add(new(pos, blocks[IndexByLocalPos(pos, chunkLength)], 1, d));
        }
        return [.. instances];
    }

    /// <summary>Culls any blocks that isn't touching air.</summary>
    /// <param name="blocks">The collection of block IDs comprising the chunk. With z > y > x index ordering.</param>
    /// <remarks>Any neighbor chunks that are not the chunk volume size will be assumed air.</remarks>
    public static FaceInstance[] CullChunk
    (
        Span<ushort> blocks,
        int chunkLength,
        Span<ushort> negZChunk,
        Span<ushort> posZChunk,
        Span<ushort> posYChunk,
        Span<ushort> negYChunk,
        Span<ushort> negXChunk,
        Span<ushort> posXChunk
    )
    {
        List<FaceInstance> instances = [];
        for (int z = 0; z < chunkLength; z++)
        for (int y = 0; y < chunkLength; y++)
        for (int x = 0; x < chunkLength; x++)
        {
            Vector3 pos = new(x, y, z);
            if (IsAir(pos, blocks, chunkLength))
                continue;
            for (int d = 0; d < 6; d++)
            {
                Vector3 blockChecking = pos + directions[d];
                Span<ushort> chunkChecking;
                if (IsLocal(blockChecking, chunkLength))
                    chunkChecking = blocks;
                else
                {
                    blockChecking = LocalPosByGlobalPos(blockChecking, chunkLength);
                    chunkChecking = d switch
                    {
                        0 => negZChunk,
                        1 => posZChunk,
                        2 => posYChunk,
                        3 => negYChunk,
                        4 => negXChunk,
                        _ => posXChunk
                    };
                }
                if (chunkChecking.Length != blocks.Length || IsAir(blockChecking, chunkChecking, chunkLength))
                    instances.Add(new(pos, blocks[IndexByLocalPos(pos, chunkLength)], 1, d));
            }
        }
        return [.. instances];
    }
}