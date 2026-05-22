
using System.Numerics;
using Silk.NET.OpenGL;

namespace GalensUnified.CubicGrid.Renderer.NET;

public class ChunkShading
{
    public const float RayEpsilon = 0.0000001f;

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
    private readonly int maxRaydistanceLocation;
    private readonly int worldOriginLocation;
    private readonly int worldDimensionsLocation;
    private readonly uint worldMaskBuffer;

    private readonly GL GL;
    private readonly Shader shader;

    public unsafe void SetWorldMask(uint[] mask, Silk.NET.Maths.Vector3D<int> dimensions, Silk.NET.Maths.Vector3D<int> origin)
    {
        GL.UseProgram(directionalComputeProgram);
        GL.Uniform3(worldDimensionsLocation, dimensions.X, dimensions.Y, dimensions.Z);
        GL.Uniform3(worldOriginLocation, origin.X, origin.Y, origin.Z);

        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, worldMaskBuffer);
        fixed (void* ptr = mask)
        {
            GL.BufferData(BufferTargetARB.ShaderStorageBuffer, (nuint)(mask.Length * sizeof(uint)), ptr, BufferUsageARB.StaticDraw);
        }
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 4, worldMaskBuffer);
        GL.UseProgram(0);
        shader.OutputErrors("Chunk Shading SetWorldMask");
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
        GL.Uniform1(maxRaydistanceLocation, maxRaydistance);
        uint regionBuffer = shader.GetBufferObjectByRegion(chunkData.RegionID);
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 0, regionBuffer);

        int numberOfBatches = chunkData.Blocks.Length / 64 + 1; // 64 is a single batch size (layout(local_size_x))
        GL.DispatchCompute((uint)numberOfBatches, 1, 1);
        GL.MemoryBarrier(MemoryBarrierMask.VertexAttribArrayBarrierBit);
        GL.UseProgram(0);
        shader.OutputErrors("Chunk Shading ShadeChunk Dispatch");
    }

    public ChunkShading(Shader shader, GL GL, string GLSLScriptsPath)
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
        maxRaydistanceLocation = GL.GetUniformLocation(directionalComputeProgram, "maxRaydistance");
        worldOriginLocation = GL.GetUniformLocation(directionalComputeProgram, "worldOrigin");
        worldDimensionsLocation = GL.GetUniformLocation(directionalComputeProgram, "worldDimensions");

        worldMaskBuffer = GL.GenBuffer();
        // Final Clean Up
        shader.OutputLogs("Chunk Shading", GL.GetProgramInfoLog(directionalComputeProgram));
        shader.OutputErrors("Chunk Shading Instantiator");
        GL.UseProgram(0);
    }
}