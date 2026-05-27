using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace GalensUnified.CubicGrid.Renderer.NET;

public class ChunkShading
{
    public const float RayEpsilon = 0.0000001f;

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
    private readonly int chunkLength;
    private readonly int uintsPerChunk;
    private bool directionalLighting = false;
    private DirectionalLightingSettings directionalLightingSettings;

    private readonly GL GL;
    private readonly Shader shader;
    private readonly HashSet<Vector3> ChunksShaded = [];

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
        ChunksShaded.Clear();
    }

    public void SetDirectionalLightingSettings(DirectionalLightingSettings? settings)
    {
        if (settings == null)
        {
            directionalLighting = false;
            return;
        }
        GL.UseProgram(directionalComputeProgram);
        directionalLighting = true;
        directionalLightingSettings = settings.Value;
        GL.Uniform1(setBaseLocation, directionalLightingSettings.setBase ? 1 : 0);
        GL.Uniform1(lightBaseLocation, directionalLightingSettings.lightBase);
        GL.Uniform1(lightHitLocation, directionalLightingSettings.lightHit);
        GL.Uniform1(lightMissLocation, directionalLightingSettings.lightMiss);
        GL.Uniform1(diffuseShadingLocation, directionalLightingSettings.diffuseShading ? 1 : 0);
        GL.Uniform3(lightDirectionLocation, -directionalLightingSettings.lightDirection);
        GL.Uniform1(lightMinLocation, directionalLightingSettings.lightMin);
        GL.Uniform1(lightMaxLocation, directionalLightingSettings.lightMax);
        GL.Uniform1(maxRaydistanceLocation, directionalLightingSettings.maxRaydistance);
        GL.UseProgram(0);
        shader.OutputErrors("Chunk Shading Set Directional Lighting Settings");
        ChunksShaded.Clear();
    }

    public void SetDirectionalLight(Vector3 direction)
    {
        if (!directionalLighting)
            throw new Exception("Directional Lighting Settings not initilized.");
        GL.UseProgram(directionalComputeProgram);
        directionalLightingSettings.lightDirection = Vector3.Normalize(new
        (
            direction.X > -RayEpsilon && direction.X < RayEpsilon ? RayEpsilon : direction.X,
            direction.Y > -RayEpsilon && direction.Y < RayEpsilon ? RayEpsilon : direction.Y,
            direction.Z > -RayEpsilon && direction.Z < RayEpsilon ? RayEpsilon : direction.Z
        ));
        GL.Uniform3(lightDirectionLocation, -directionalLightingSettings.lightDirection);
        GL.UseProgram(0);
        shader.OutputErrors("Chunk Shading Set Directional Light");
        ChunksShaded.Clear();
    }

    private nint ChunkByteOffsetByPosition(Vector3D<int> pos)
    {
        pos -= worldOrigin;
        pos /= chunkLength;
        int chunkIndex = (pos.Z * WorldDimensionsInChunks.Y + pos.Y) * WorldDimensionsInChunks.X + pos.X;
        return (nint)chunkIndex * uintsPerChunk * sizeof(uint);
    }

    /// <remarks>
    /// Diffuse shading only gets applied to light hit. If the light isn't providing the light, there's nothing to diffuse.
    /// </remarks>
    public void DirectionalShadeChunk(Vector3 chunk)
    {
        if (!directionalLighting || ChunksShaded.Contains(chunk))
            return;
        GL.UseProgram(directionalComputeProgram);
        Shader.ChunkRenderingData chunkData = shader.chunkByPos[chunk];
        GL.Uniform1(bufferStartLocation, chunkData.RegionInstanceIndex);
        GL.Uniform1(bufferEndLocation, chunkData.RegionInstanceIndex + (uint)chunkData.Blocks.Length);
        GL.Uniform3(chunkPosLocation, chunk);
        uint regionBuffer = shader.GetBufferObjectByRegion(chunkData.RegionID);
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 0, regionBuffer);

        int numberOfBatches = chunkData.Blocks.Length / 64 + 1; // 64 is a single batch size (layout(local_size_x))
        GL.DispatchCompute((uint)numberOfBatches, 1, 1);
        GL.MemoryBarrier(MemoryBarrierMask.VertexAttribArrayBarrierBit);
        GL.UseProgram(0);
        shader.OutputErrors("Chunk Shading ShadeChunk Dispatch");
        ChunksShaded.Add(chunk);
    }

    public ChunkShading(Shader shader, GL GL, string GLSLScriptsPath, int chunkLength, Vector3D<int> worldDimensionsInChunks)
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

        this.chunkLength = chunkLength;
        this.uintsPerChunk = chunkLength * chunkLength * chunkLength / 32;
        GL.Uniform1(chunkLengthLocation, chunkLength);

        worldMaskBuffer = GL.GenBuffer();
        SetWorldDimensions(worldDimensionsInChunks);
        // Final Clean Up
        shader.OutputLogs("Chunk Shading", GL.GetProgramInfoLog(directionalComputeProgram));
        shader.OutputErrors("Chunk Shading Instantiator");
        GL.UseProgram(0);
    }
}