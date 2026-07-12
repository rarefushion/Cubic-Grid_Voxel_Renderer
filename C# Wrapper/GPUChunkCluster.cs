using GalensUnified.CubicGrid.Core;
using GalensUnified.CubicGrid.Core.Math;
using GalensUnified.Graphics.Buffers;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace GalensUnified.CubicGrid.Framework;

/// <summary>
/// Manages a specified number of chunks.<br/>
/// Uses one flattened array of ushorts for every chunk.
/// Indexes are generate to represent a position,
/// the positions are first wrapped to be contained within the cluster.
/// E.g a chunk at X:0, Y:0 Z:0 will have the same index as a chunk at X:(the cluster's length) Y:0 Z:0.
/// </summary>
public class GPUChunkCluster<TChunkDims>
where TChunkDims : IChunkDims
{
    public readonly int chunkCount;
    public readonly int clusterChunkLength;
    public readonly int clusterChunkHeight;
    public readonly int clusterLength;
    public readonly int clusterHeight;
    public readonly int blockCount;

    public readonly ShaderStorageBufferObject<ushort> flattenedChunksSSBO;

    private readonly GL GL;

    /// <summary>Assigns each block of a chunk.</summary>
    /// <param name="chunkPos"></param>
    /// <param name="blocks">The Span of ushort chunk to upload to the gpu.</param>
    public void UploadChunk(Span<ushort> blocks, Vector3D<int> chunkPos)
    {
        flattenedChunksSSBO.SetSubData([.. blocks], IndexByChunkCoord(ChunkCoordByGlobalPos(chunkPos)) * sizeof(ushort), true);
        GLEnum err;
        while ((err = GL.GetError()) != GLEnum.NoError)
            throw new Exception($"OpenGL Error: {err}");
    }

    /// <summary>Clears an entire chunk.</summary>
    /// <param name="chunkPos">The chunk position to clear.</param>
    /// <param name="clearValue">The value to clear the chunk with. Default is 0.</param>
    public void ClearChunk(Vector3D<int> chunkPos, ushort clearValue = 0)
    {
        flattenedChunksSSBO.Bind();
        int chunkIndex = IndexByChunkCoord(ChunkCoordByGlobalPos(chunkPos));
        uint size = (uint)TChunkDims.Volume * sizeof(ushort);
        GL.ClearBufferSubData
        (
            BufferTargetARB.ShaderStorageBuffer,
            SizedInternalFormat.R16ui,
            chunkIndex * sizeof(ushort),
            size,
            PixelFormat.RedInteger,
            PixelType.UnsignedShort,
            ref clearValue
        );
        GLEnum err;
        while ((err = GL.GetError()) != GLEnum.NoError)
            throw new Exception($"OpenGL Error: {err}");
    }

    /// <summary>
    /// Calculates the chunk coordinate (grid address) by dividing a position by the chunk size.
    /// First wrapping the position into the local world space.
    /// </summary>
    public Vector3D<int> ChunkCoordByGlobalPos(Vector3D<int> pos) =>
        ChunkCoordByLocalPos(LocalPosByGlobalPos(pos));


    public Vector3D<int> LocalPosByGlobalPos(Vector3D<int> pos) => new
        (
            ((pos.X % clusterLength) + clusterLength) % clusterLength,
            ((pos.Y % clusterHeight) + clusterHeight) % clusterHeight,
            ((pos.Z % clusterLength) + clusterLength) % clusterLength
        );

    /// <summary>Calculates the chunk coordinate (grid address) by dividing a position by the chunk size.</summary>
    public Vector3D<int> ChunkCoordByLocalPos(Vector3D<int> pos) => new
    (
        pos.X >> ChunkMath<TChunkDims>.shift,
        pos.Y >> ChunkMath<TChunkDims>.shift,
        pos.Z >> ChunkMath<TChunkDims>.shift
    );

    /// <summary>Calculates the 1D index of a chunk coordinate (grid address).</summary>
    public int IndexByChunkCoord(Vector3D<int> coord) =>
        ((coord.Z * clusterChunkHeight + coord.Y) * clusterChunkLength + coord.X) * TChunkDims.Volume;


    /// <param name="clusterChunkLength">Number of chunks along the X and Z axis.</param>
    /// <param name="clusterChunkHeight">Number of chunks along the Y axis.</param>
    /// <param name="bindingIndex">The binding the ushort buffer is bound to.</param>
    public GPUChunkCluster(GL GL, int clusterChunkLength, int clusterChunkHeight, uint bindingIndex = 1)
    {
        this.GL = GL;
        this.clusterChunkLength = clusterChunkLength;
        this.clusterChunkHeight = clusterChunkHeight;
        this.chunkCount = checked(clusterChunkLength * clusterChunkHeight * clusterChunkLength);
        this.clusterLength = clusterChunkLength * TChunkDims.Length;
        this.clusterHeight = clusterChunkHeight * TChunkDims.Length;
        this.blockCount = checked(TChunkDims.Volume * chunkCount);
        this.flattenedChunksSSBO = new(GL, BufferUsageARB.DynamicDraw, bindingIndex, (uint)blockCount * sizeof(ushort));
    }
}