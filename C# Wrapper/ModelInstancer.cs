using System.Numerics;
using GalensUnified.CubicGrid.Core;
using GalensUnified.CubicGrid.Framework;
using GalensUnified.Graphics;
using GalensUnified.Graphics.Buffers;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace GalensUnified.CubicGrid.Renderer.NET;

public class ModelInstancer<TChunkDims>
where TChunkDims : IChunkDims
{
    public uint computeShader;

    private readonly int chunkOffsetLoc;
    private readonly int chunkPosLoc;
    private readonly int instanceOffsetLoc;
    private readonly uint shapeCountID;

    private readonly GL GL;
    private readonly ShaderStorageBufferObject<InstancerRenderData> renderDataBuf;
    private readonly GPUChunkCluster<TChunkDims> chunkCluster;
    private readonly Shader shader;

    public void ComputeInstances(Vector3D<int> chunkPos)
    {
        GL.UseProgram(computeShader);
        GL.Uniform3(chunkPosLoc, (Vector3)chunkPos);
        int chunkIndex = chunkCluster.IndexByChunkCoord(chunkCluster.ChunkCoordByGlobalPos(chunkPos));
        GL.Uniform1(chunkOffsetLoc, chunkIndex);
        // Instances
        Shader.BufferRental rental = shader.RentBuffer();
        GL.Uniform1(instanceOffsetLoc, rental.ByteOffset / ShapeInstance.MemorySize);
        rental.SSBO.BindBase();
        // Blocks
        chunkCluster.flattenedChunksSSBO.BindBase();
        // Render Data
        renderDataBuf.BindBase();
        // Counter
        uint shapeCount = 0;
        GL.BindBufferBase(BufferTargetARB.AtomicCounterBuffer, 5, shapeCountID);
        GL.BufferSubData(BufferTargetARB.AtomicCounterBuffer, IntPtr.Zero, sizeof(uint), ref shapeCount);
        // Dispatch
        uint groups = (uint)TChunkDims.Length / 8;
        GL.DispatchCompute(groups, groups, groups);
        GL.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);
        GL.MemoryBarrier(MemoryBarrierMask.AtomicCounterBarrierBit);
        shapeCount = GL.GetBufferSubData<uint>(GLEnum.AtomicCounterBuffer, IntPtr.Zero, sizeof(uint));
        shader.ReturnRental(rental, (int)shapeCount * ShapeInstance.MemorySize, (Vector3)chunkPos);
        GLEnum err;
        while ((err = GL.GetError()) != GLEnum.NoError)
            throw new Exception($"OpenGL Error: {err}");
    }

    public ModelInstancer(GL GL, string GLSLScriptsPath, GPUChunkCluster<TChunkDims> chunkCluster, Shader shader)
    {
        this.GL = GL;
        this.chunkCluster = chunkCluster;
        this.shader = shader;
        string instancerShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "ModelInstancer.glsl"));
        Shaders.ShaderScript instancer = new(instancerShaderCode, ShaderType.ComputeShader);
        computeShader = Shaders.CreateShaderProgram(GL, [ instancer ]);
        GL.UseProgram(computeShader);
        GLEnum err;
        while ((err = GL.GetError()) != GLEnum.NoError)
            throw new Exception($"OpenGL Error: {err}");

        GL.Uniform1(GL.GetUniformLocation(computeShader, "chunkLength"), TChunkDims.Length);
        GL.Uniform1(GL.GetUniformLocation(computeShader, "chunkVolume"), TChunkDims.Volume);
        chunkOffsetLoc = GL.GetUniformLocation(computeShader, "chunkOffset");
        chunkPosLoc = GL.GetUniformLocation(computeShader, "chunkPos");
        instanceOffsetLoc = GL.GetUniformLocation(computeShader, "instanceOffset");
        GL.Uniform1(GL.GetUniformLocation(computeShader, "clusterLength"), chunkCluster.clusterLength);
        GL.Uniform1(GL.GetUniformLocation(computeShader, "clusterHeight"), chunkCluster.clusterHeight);
        GL.Uniform1(GL.GetUniformLocation(computeShader, "clusterChunkLength"), chunkCluster.clusterChunkLength);
        GL.Uniform1(GL.GetUniformLocation(computeShader, "clusterChunkHeight"), chunkCluster.clusterChunkHeight);

        InstancerRenderData[] datas = [.. BlockRenderData.renderDataByBlock.Select((d, i) => new InstancerRenderData
        (
            d.faceBack,
            d.faceFront,
            d.faceTop,
            d.faceBottom,
            d.faceLeft,
            d.faceRight,
            0
        ))];
        renderDataBuf = new(GL, BufferUsageARB.StaticRead, 4, datas);

        shapeCountID = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.AtomicCounterBuffer, shapeCountID);
        uint shapeCount = 0;
        GL.BufferData(BufferTargetARB.AtomicCounterBuffer, sizeof(uint), ref shapeCount, BufferUsageARB.DynamicCopy);
    }

    public readonly struct InstancerRenderData(int faceBack, int faceFront, int faceTop, int faceBottom, int faceLeft, int faceRight, int shape)
    {
        readonly int faceBack = faceBack, faceFront = faceFront, faceTop = faceTop, faceBottom = faceBottom, faceLeft = faceLeft, faceRight = faceRight;
        readonly int shape = shape;
    }
}