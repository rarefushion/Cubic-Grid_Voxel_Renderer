using Silk.NET.Maths;

/// <summary>
/// Manages a specified number of chunks allowing retrieval via their position.
/// </summary>
public class ChunkCluster
{
    public readonly int chunkLength;
    public readonly int chunkVolume;
    public readonly int chunkCount;
    public readonly Vector3D<int> chunkCountDimensions;
    public readonly Vector3D<int> clusterLengthDimensions;

    private readonly int[] chunks;

    public Vector3D<int> ChunkCoordByLocalPos(Vector3D<float> P) => new
        (
            (int)MathF.Floor(P.X / chunkLength),
            (int)MathF.Floor(P.Y / chunkLength),
            (int)MathF.Floor(P.Z / chunkLength)
        );

    public Vector3D<int> LocalChunkPosByLocalPos(Vector3D<int> P) =>
        ChunkCoordByLocalPos(P.As<float>()) * chunkLength;

    public Vector3D<int> LocalPosByGlobalPos(Vector3D<int> P) => new
        (
            ((P.X % clusterLengthDimensions.X) + clusterLengthDimensions.X) % clusterLengthDimensions.X,
            ((P.Y % clusterLengthDimensions.Y) + clusterLengthDimensions.Y) % clusterLengthDimensions.Y,
            ((P.Z % clusterLengthDimensions.Z) + clusterLengthDimensions.Z) % clusterLengthDimensions.Z
        );

    // This might be incorrect.
    // public Vector3D<int> ChunkPosByGlobalPos(Vector3D<int> P) =>
    //     LocalChunkPosByLocalPos(LocalPosByGlobalPos(P)) + (P - LocalPosByGlobalPos(P));

    public int IndexByChunkCoord(Vector3D<int> C) =>
        ((C.Z * chunkCountDimensions.Z + C.Y) * chunkCountDimensions.Y + C.X) * chunkVolume;

    private int IndexByLocalPos(Vector3D<int> P) =>
        IndexByChunkCoord(ChunkCoordByLocalPos(P.As<float>()));

    public int IndexByGlobalPos(Vector3D<int> P) =>
        IndexByLocalPos(LocalPosByGlobalPos(P));

    public Span<int> GetChunkByIndex(int index) =>
        chunks.AsSpan(index, chunkVolume);

    public Span<int> GetChunkByPosition(Vector3D<int> pos) =>
        GetChunkByIndex(IndexByGlobalPos(pos));

    public void RemoveChunk(Vector3D<int> pos) =>
        GetChunkByPosition(pos).Clear();


    /// <param name="chunkLength"> The length of a single chunk. In other words the cube root of the chunk's volume. </param>
    /// <param name="clusterDimensions"> Number of chunks along each axis, allowing for a non cubic cluster. </param>
    public ChunkCluster(int chunkLength, Vector3D<int> clusterDimensions)
    {
        this.chunkLength = chunkLength;
        this.chunkVolume = chunkLength * chunkLength * chunkLength;
        this.chunkCountDimensions = clusterDimensions;
        this.chunkCount = (int)clusterDimensions.X * (int)clusterDimensions.Y * (int)clusterDimensions.Z;
        this.clusterLengthDimensions = new Vector3D<int>
        (
            chunkCountDimensions.X * chunkLength,
            chunkCountDimensions.Y * chunkLength,
            chunkCountDimensions.Z * chunkLength
        );
        this.chunks = new int[chunkVolume * chunkCount];
    }
}