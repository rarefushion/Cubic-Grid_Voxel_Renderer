using System.Numerics;
using GalensUnified.CubicGrid.Core;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace GalensUnified.CubicGrid.Renderer.NET;

public class ChunkShading<TChunkDims> where TChunkDims : IChunkDims
{
    public const float RayEpsilon = 0.0000001f;
    public static readonly int uintsPerChunk = TChunkDims.Volume / 32;

    public Vector3D<int> WorldDimensionsInChunks { get; private set; }
    private Vector3D<int> worldOrigin;

    private readonly uint directionalComputeProgram;
    private readonly int bufferStartLocation;
    private readonly int bufferEndLocation;
    private readonly int chunkPosLocation;
    private readonly int setBaseLocation;
    private readonly int lightBaseLocation;
    private readonly int lightHitLocation;
    private readonly int lightMissLocation;
    private readonly int lightDirectionLocation;
    private readonly int diffuseShadingLocation;
    private readonly int lightMinLocation;
    private readonly int lightMaxLocation;
    private readonly int chunkLengthLocation;
    private readonly int maxRaydistanceLocation;
    private readonly int worldOriginLocation;
    private readonly int worldDimensionsLocation;
    private readonly uint worldMaskBuffer;

    private readonly GL GL;
    private readonly Shader shader;

    public void SetWorldOriginPosition(Vector3D<int> origin)
    {
        worldOrigin = origin;
        GL.UseProgram(directionalComputeProgram);
        GL.Uniform3(worldOriginLocation, origin.X, origin.Y, origin.Z);
        GL.UseProgram(0);
    }

    public unsafe void SetWorldDimensions(Vector3D<int> dimensions)
    {
        WorldDimensionsInChunks = dimensions;
        GL.UseProgram(directionalComputeProgram);
        GL.Uniform3(worldDimensionsLocation, dimensions.X, dimensions.Y, dimensions.Z);

        int totalChunks = dimensions.X * dimensions.Y * dimensions.Z;
        nuint totalBytes = (nuint)totalChunks * (nuint)uintsPerChunk * sizeof(uint);
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, worldMaskBuffer);
        GL.BufferData(BufferTargetARB.ShaderStorageBuffer, totalBytes, null, BufferUsageARB.DynamicDraw);
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 4, worldMaskBuffer);
        GL.UseProgram(0);
        shader.OutputErrors("Chunk Shading SetWorldDimensions");
    }

    public unsafe void SetChunkMask(Vector3D<int> position, uint[] mask)
    {
        GL.UseProgram(directionalComputeProgram);
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, worldMaskBuffer);
        nint worldIndex = ChunkByteOffsetByPosition(position);
        fixed (void* ptr = mask)
        {
            GL.BufferSubData(BufferTargetARB.ShaderStorageBuffer, worldIndex, (nuint)(mask.Length * sizeof(uint)), ptr);
        }
        GL.UseProgram(0);
        shader.OutputErrors("Chunk Shading SetChunkMask");
    }

    private nint ChunkByteOffsetByPosition(Vector3D<int> pos)
    {
        pos -= worldOrigin;
        pos /= TChunkDims.Length;
        int chunkIndex = (pos.Z * WorldDimensionsInChunks.Y + pos.Y) * WorldDimensionsInChunks.X + pos.X;
        return (nint)chunkIndex * uintsPerChunk * sizeof(uint);
    }

    /// <remarks>
    /// Diffuse shading only gets applied to light hit. If the light isn't providing the light, there's nothing to diffuse.
    /// </remarks>
    public void DirectionalShadeChunk
    (
        Vector3 chunk,
        bool setBase,
        float lightBase,
        float lightHit,
        float lightMiss,
        Vector3 lightDirection,
        bool diffuseShading,
        float lightMin,
        float lightMax,
        int maxRaydistance
    )
    {
        lightDirection = Vector3.Normalize(new
        (
            lightDirection.X > -RayEpsilon && lightDirection.X < RayEpsilon ? RayEpsilon : lightDirection.X,
            lightDirection.Y > -RayEpsilon && lightDirection.Y < RayEpsilon ? RayEpsilon : lightDirection.Y,
            lightDirection.Z > -RayEpsilon && lightDirection.Z < RayEpsilon ? RayEpsilon : lightDirection.Z
        ));
        GL.UseProgram(directionalComputeProgram);
        Shader.ChunkRenderingData chunkData = shader.chunkByPos[chunk];
        GL.Uniform1(bufferStartLocation, chunkData.RegionInstanceIndex);
        GL.Uniform1(bufferEndLocation, chunkData.RegionInstanceIndex + (uint)chunkData.Blocks.Length);
        GL.Uniform3(chunkPosLocation, chunk);
        GL.Uniform1(setBaseLocation, setBase ? 1 : 0);
        GL.Uniform1(lightBaseLocation, lightBase);
        GL.Uniform1(lightHitLocation, lightHit);
        GL.Uniform1(lightMissLocation, lightMiss);
        GL.Uniform1(diffuseShadingLocation, diffuseShading ? 1 : 0);
        GL.Uniform3(lightDirectionLocation, lightDirection);
        GL.Uniform1(lightMinLocation, lightMin);
        GL.Uniform1(lightMaxLocation, lightMax);
        GL.Uniform1(chunkLengthLocation, TChunkDims.Length);
        GL.Uniform1(maxRaydistanceLocation, maxRaydistance);
        uint regionBuffer = shader.GetBufferObjectByRegion(chunkData.RegionID);
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 0, regionBuffer);

        int numberOfBatches = chunkData.Blocks.Length / 64 + 1; // 64 is a single batch size (layout(local_size_x))
        GL.DispatchCompute((uint)numberOfBatches, 1, 1);
        GL.MemoryBarrier(MemoryBarrierMask.VertexAttribArrayBarrierBit);
        GL.UseProgram(0);
        shader.OutputErrors("Chunk Shading ShadeChunk Dispatch");
    }

    public ChunkShading(Shader shader, GL GL, string GLSLScriptsPath, Vector3D<int> worldDimensionsInChunks)
    {
        this.shader = shader;
        this.GL = GL;
        //Create Shader
        string raymarchShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "RaymarchBitmask.glsl"));
        uint raymarchShader = GL.CreateShader(ShaderType.ComputeShader);
        GL.ShaderSource(raymarchShader, raymarchShaderCode);
        GL.CompileShader(raymarchShader);
        string shadingComputeCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "DirectionalShadingCompute.glsl"));
        uint computeShader = GL.CreateShader(ShaderType.ComputeShader);
        GL.ShaderSource(computeShader, shadingComputeCode);
        GL.CompileShader(computeShader);
        directionalComputeProgram = GL.CreateProgram();
        GL.AttachShader(directionalComputeProgram, raymarchShader);
        GL.AttachShader(directionalComputeProgram, computeShader);
        GL.LinkProgram(directionalComputeProgram);
        // Verify
        GL.GetProgram(directionalComputeProgram, GLEnum.LinkStatus, out int success);
        if (success == 0)
           Console.WriteLine("Shading Compute link failed: " + GL.GetProgramInfoLog(directionalComputeProgram));
        // Clean Up
        GL.DetachShader(directionalComputeProgram, raymarchShader);
        GL.DetachShader(directionalComputeProgram, computeShader);
        GL.DeleteShader(raymarchShader);
        GL.DeleteShader(computeShader);
        GL.UseProgram(directionalComputeProgram);
        // Get Uniform Locations
        bufferStartLocation = GL.GetUniformLocation(directionalComputeProgram, "bufferStart");
        bufferEndLocation = GL.GetUniformLocation(directionalComputeProgram, "bufferEnd");
        chunkPosLocation = GL.GetUniformLocation(directionalComputeProgram, "chunkPos");
        setBaseLocation = GL.GetUniformLocation(directionalComputeProgram, "setBase");
        lightBaseLocation = GL.GetUniformLocation(directionalComputeProgram, "lightBase");
        lightHitLocation = GL.GetUniformLocation(directionalComputeProgram, "lightHit");
        lightMissLocation = GL.GetUniformLocation(directionalComputeProgram, "lightMiss");
        lightDirectionLocation = GL.GetUniformLocation(directionalComputeProgram, "lightDirection");
        diffuseShadingLocation = GL.GetUniformLocation(directionalComputeProgram, "diffuseShading");
        lightMinLocation = GL.GetUniformLocation(directionalComputeProgram, "lightMin");
        lightMaxLocation = GL.GetUniformLocation(directionalComputeProgram, "lightMax");
        chunkLengthLocation = GL.GetUniformLocation(directionalComputeProgram, "chunkLength");
        maxRaydistanceLocation = GL.GetUniformLocation(directionalComputeProgram, "maxRaydistance");
        worldOriginLocation = GL.GetUniformLocation(directionalComputeProgram, "worldOrigin");
        worldDimensionsLocation = GL.GetUniformLocation(directionalComputeProgram, "worldDimensionsInChunks");

        worldMaskBuffer = GL.GenBuffer();
        SetWorldDimensions(worldDimensionsInChunks);
        // Final Clean Up
        shader.OutputLogs("Chunk Shading", GL.GetProgramInfoLog(directionalComputeProgram));
        shader.OutputErrors("Chunk Shading Instantiator");
        GL.UseProgram(0);
    }
}