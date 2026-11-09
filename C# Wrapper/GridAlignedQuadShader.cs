using System.Numerics;
using GalensUnified.CubicGrid.Core;
using GalensUnified.Graphics;
using GalensUnified.Graphics.Buffers;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace GalensUnified.CubicGrid.Renderer.NET;

/// <summary> Instances and draws quads that are aligned to the cubic grid of given chunks.</summary>
public class GridAlignedQuadShader
{
    public record ChunkRenderingData(Vector3 Position, GridAlignedQuadInstance[] Quads, Vector3[] Tints, int RegionInstanceIndex, int RegionID);

    public readonly Dictionary<Vector3, ChunkRenderingData> chunkByPos = [];
    public Action<string>? OutputLog;
    public Action<string>? OutputError;
    public uint shaderProgram;

    private readonly int chunkLength;
    private readonly int projectionLocation;
    private readonly int viewLocation;
    private readonly int chunkPosLocation;
    private readonly uint tbo;
    private readonly uint bufferSize;
    private readonly uint tintsBufferSize;

    private readonly GL GL;
    private readonly Dictionary<int, RegionBuffer> regionByID = [];
    private int currentRegionID = 0;

    /// <summary>Registers a chunk for rendering and assignes them to a VBO to render.</summary>
    /// <param name="position">The world-space position of the chunk.</param>
    /// <param name="quads">The collection of quad instances to render.</param>
    /// <param name="tints">The collection of tints for each instance. Must be same Length as <paramref name="quads"/>. Alpha/w is unused.</param>
    public unsafe void RenderChunk(Vector3 position, GridAlignedQuadInstance[] quads, Vector3[] tints)
    {
        GL.UseProgram(shaderProgram);
        nuint size = (nuint)(quads.Length * GridAlignedQuadInstance.MemorySize);
        nuint tintsSize = (nuint)(tints.Length * sizeof(Vector3));
        if (!regionByID[currentRegionID].CanFit(size, tintsSize))
            NewRegion();
        int index = regionByID[currentRegionID].QuadsPointer;
        int tintsIndex = regionByID[currentRegionID].TintsPointer;
        for (int i = 0; i < quads.Length; i++)
        {
            quads[i] = new
            (
                quads[i].posX, quads[i].posY, quads[i].posZ,
                (Direction)quads[i].normal,
                quads[i].texture,
                quads[i].scaleX, quads[i].scaleY,
                (uint)(quads[i].tintIndex + tintsIndex / sizeof(Vector3))
            );
        }
        regionByID[currentRegionID].Quads.SetSubData(quads, index, true);
        regionByID[currentRegionID].Tints.SetSubData(tints, tintsIndex, true);
        ChunkRenderingData chunk = new(position, quads, tints, index / GridAlignedQuadInstance.MemorySize, currentRegionID);
        regionByID[currentRegionID].QuadsPointer += (int)size;
        regionByID[currentRegionID].TintsPointer += (int)tintsSize;
        regionByID[currentRegionID].Chunks.Add(position);
        chunkByPos[position] = chunk;
        OutputErrors("Quad Mat Creating Chunk");
    }

    /// <summary>Deregisters a chunk for rendering, freeing it to be overwritten.</summary>
    public void DeactivateChunk(Vector3 position)
    {
        if (!chunkByPos.Remove(position, out ChunkRenderingData? chunk))
            return;
        regionByID[chunk.RegionID].Chunks.Remove(position);
        if (regionByID[chunk.RegionID].Chunks.Count == 0 && chunk.RegionID != currentRegionID)
        {
            GL.DeleteBuffer(regionByID[chunk.RegionID].Quads.Handle);
            regionByID.Remove(chunk.RegionID);
        }
        OutputErrors("Quad Mat DeactivateChunk");
    }

    private void NewRegion()
    {
        ShaderStorageBufferObject<GridAlignedQuadInstance> SSBO = new(GL, BufferUsageARB.DynamicDraw, 0, bufferSize);
        ShaderStorageBufferObject<Vector3> tints = new(GL, BufferUsageARB.DynamicDraw, 1, tintsBufferSize);
        regionByID.Add(++currentRegionID, new RegionBuffer(SSBO, tints, bufferSize, tintsBufferSize));
        OutputErrors("Quad Mat Creating Region");
    }

    /// <summary>Executes the rendering pass for all registered chunks that pass the occlusion test.</summary>
    public unsafe void Render(ICamera cam)
    {
        Matrix4x4 viewMatrix = CameraMatrices.CreateViewMatrix(Vector3.Zero, cam.EurlerAngles.X, cam.EurlerAngles.Y, cam.EurlerAngles.Z);
        Matrix4x4 projectionMatrix = CameraMatrices.CreateProjectionMatrix(cam.Fov, cam.AspectRatio, cam.NearPlane, cam.FarPlane);
        // Render
        GL.UseProgram(shaderProgram);
        GL.UniformMatrix4(projectionLocation, 1, false, (float*)&projectionMatrix);
        GL.UniformMatrix4(viewLocation, 1, false, (float*)&viewMatrix);
        MatrixPlanes.Plane[] planes = MatrixPlanes.ViewFrustum(viewMatrix, projectionMatrix);

        GL.BindTexture(GLEnum.Texture2DArray, tbo);
        foreach (RegionBuffer region in regionByID.Values)
        {
            region.Bind();
            OutputErrors("Voxel Mat Bind");
            foreach (ChunkRenderingData chunk in region.Chunks.Select(p => chunkByPos[p]))
            {
                Vector3 offsetPos = chunk.Position - cam.Position;
                if (!MatrixPlanes.IsBoxInFrustum(planes, offsetPos, offsetPos + Vector3.One * chunkLength))
                    continue;
                GL.Uniform3(chunkPosLocation, offsetPos);
                GL.DrawArraysInstancedBaseInstance(PrimitiveType.Triangles, 0, 6, (uint)chunk.Quads.Length, (uint)chunk.RegionInstanceIndex);
            }
            OutputErrors("Voxel Mat Chunks");
        }
        OutputErrors("Voxel Mat Render");
    }

    /// <summary>Initializes the GridAlignedQuadShader by compiling shaders, allocating GPU buffers, and building the texture array.</summary>
    /// <param name="openGL">The GL interface for executing commands.</param>
    /// <param name="GLSLScriptsPath">The directory path containing the .glsl shader files.</param>
    /// <param name="chunkLength">The width/height/depth of a single chunk in blocks.</param>
    /// <param name="vramBufferRegionSize">Vram batch size in bytes to reserve.</param>
    /// <param name="cameraNearPlane">The distance to the camera's near clipping plane.</param>
    /// <param name="imageByTextureID">Maps <see cref="Image"/> textures to a specific textureID.</param>
    /// <param name="errorAction">An optional delegate for handling error messages.</param>
    /// <param name="logAction">An optional delegate for handling shader compilation logs.</param>
    public unsafe GridAlignedQuadShader
    (
        GL openGL,
        string GLSLScriptsPath,
        int chunkLength,
        int vramBufferRegionSize,
        float cameraNearPlane,
        Image[] imageByTextureID,
        Action<string>? errorAction = null,
        Action<string>? logAction = null
    )
    {
        GL = openGL;
        OutputError = errorAction;
        OutputLog = logAction;
        //Create Shader
        string vertexShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "GridAlignedQuadVertex.glsl"));
        Shaders.ShaderScript Vertex = new(vertexShaderCode, ShaderType.VertexShader);
        string fragmentShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "Fragment.glsl"));
        Shaders.ShaderScript Fragment = new(fragmentShaderCode, ShaderType.FragmentShader);
        shaderProgram = Shaders.CreateShaderProgram(GL, [Vertex, Fragment]);
        GL.UseProgram(shaderProgram);
        // Assing shader variables
        projectionLocation = GL.GetUniformLocation(shaderProgram, "Uniforms.projection");
        viewLocation = GL.GetUniformLocation(shaderProgram, "Uniforms.view");
        this.chunkLength = chunkLength;
        int chunkVolume = chunkLength * chunkLength * chunkLength;
        chunkPosLocation = GL.GetUniformLocation(shaderProgram, "Uniforms.chunkPos");
        // Region Buffers
        int maxSSBOSize = GL.GetInteger(GLEnum.MaxShaderStorageBlockSize);
        if (vramBufferRegionSize > maxSSBOSize)
            throw new Exception($"vramBufferRegionSize size exceeds hardware's allowed size of {maxSSBOSize}");
        int chunkVolumeSize = GridAlignedQuadInstance.MemorySize * chunkVolume;
        if (vramBufferRegionSize < chunkVolumeSize)
            throw new Exception($"vramBufferRegionSize size less than a single chunk. Min {chunkVolumeSize}");
        int waste = vramBufferRegionSize % chunkVolumeSize;
        if (waste > 0)
            OutputLogs("Quad Mat Instantiator", $"vramBufferRegionSize doesn't align with chunk size {chunkVolumeSize} and wastes {waste} bytes.");
        bufferSize = (uint)vramBufferRegionSize;
        tintsBufferSize = (uint)sizeof(Vector3) * (bufferSize / GridAlignedQuadInstance.MemorySize);
        currentRegionID = -1;
        NewRegion();

        // Textures
        uint maxX = 0, maxY = 0;
        foreach (Image img in imageByTextureID)
        {
            maxX = (uint)Math.Max(maxX, img.Width);
            maxY = (uint)Math.Max(maxY, img.Height);
        }
        uint tbo;
        GL.GenTextures(1, &tbo);
        GL.BindTexture(GLEnum.Texture2DArray, tbo);
        GL.TexImage3D(GLEnum.Texture2DArray, 0, (int)GLEnum.Rgba, maxX, maxY, (uint)imageByTextureID.Length, 0, GLEnum.Rgba, GLEnum.UnsignedByte, null);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        for (int i = 0; i < imageByTextureID.Length; i++)
        {
            Image img = imageByTextureID[i];
            GL.TexSubImage3D
            (
                GLEnum.Texture2DArray,
                0,
                0,
                0,
                i,
                (uint)img.Width,
                (uint)img.Height,
                1,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                img.Pixels
            );
        }
        this.tbo = tbo;
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "textureArray"), 0);

        OutputLogs("Shader", GL.GetProgramInfoLog(shaderProgram));
        OutputErrors("Quad Mat Instantiator");
    }

    private void OutputErrors(string location)
    {
        GLEnum err;
        while ((err = GL.GetError()) != GLEnum.NoError)
            OutputError?.Invoke($"OpenGL Error @{location}: {err}");
    }

    private void OutputLogs(string location, string log)
    {
        if (string.IsNullOrEmpty(log))
            return;
        OutputLog?.Invoke($"OpenGL Log @{location}: {log}");
    }


    private class RegionBuffer(ShaderStorageBufferObject<GridAlignedQuadInstance> Quads, ShaderStorageBufferObject<Vector3> Tints, uint BufferSize, uint TintBufferSize)
    {
        public readonly ShaderStorageBufferObject<GridAlignedQuadInstance> Quads = Quads;
        public readonly ShaderStorageBufferObject<Vector3> Tints = Tints;
        public int QuadsPointer = 0;
        public int TintsPointer = 0;
        public readonly HashSet<Vector3> Chunks = [];

        public bool CanFit(nuint size, nuint tintSize) =>
            (nuint)QuadsPointer + size < BufferSize && (nuint)TintsPointer + tintSize < TintBufferSize;

        public void Bind()
        {
            Quads.BindBase();
            Tints.BindBase();
        }
    }
}