using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace GalensUnified.CubicGrid.Renderer.NET;

public class Shader
{
    public record ChunkRenderingData(Vector3 Position, CubeFaceInstance[] Blocks, int RegionInstanceIndex, int RegionID);

    public readonly Dictionary<Vector3, ChunkRenderingData> chunkByPos = [];
    public Action<string>? OutputLog;
    public Action<string>? OutputError;
    public uint shaderProgram;

    private readonly int chunkLength;
    private readonly int chunkVolume;
    private readonly int projectionLocation;
    private readonly int viewLocation;
    private readonly int chunkPosLocation;
    private readonly uint tbo;
    private readonly uint bufferSize;
    private readonly nint memBlockInstanceTextureOffset;
    private readonly nint memBlockInstanceTintOffset;
    private readonly nint memBlockInstanceFaceOffset;

    private readonly GL GL;
    private readonly Dictionary<int, RegionBuffer> regionByID = [];
    private int currentRegionID = 0;

    /// <summary>Registers or replaces a chunk for rendering and assignes them to a VBO to render.</summary>
    /// <param name="position">The world-space position of the chunk.</param>
    /// <param name="blocks">The collection of block instances to render.</param>
    public unsafe void RenderChunk(Vector3 position, CubeFaceInstance[] blocks)
    {
        GL.UseProgram(shaderProgram);
        nuint size = (nuint)(blocks.Length * CubeFaceInstance.MemorySize);
        if (!regionByID[currentRegionID].CanFit(size))
            NewRegion();
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, regionByID[currentRegionID].Vbo);
        GL.BindVertexArray(regionByID[currentRegionID].Vao);
        int index = regionByID[currentRegionID].BytePointer;
        ChunkRenderingData chunk = new(position, blocks, index / CubeFaceInstance.MemorySize, currentRegionID);
        fixed (void* buf = blocks.ToArray())
        {
            GL.BufferSubData(BufferTargetARB.ArrayBuffer, index, size, buf);
        }
        regionByID[currentRegionID].BytePointer += (int)size;
        regionByID[currentRegionID].Chunks.Add(position);
        chunkByPos[position] = chunk;
        OutputErrors("Voxel Mat Creating Chunk");
    }

    /// <summary>Deregisters a chunk for rendering, freeing it to be overwritten.</summary>
    public unsafe void DeactivateChunk(Vector3 position)
    {
        if (!chunkByPos.Remove(position, out ChunkRenderingData? chunk))
            return;
        regionByID[chunk.RegionID].Chunks.Remove(position);
        if (regionByID[chunk.RegionID].Chunks.Count == 0 && chunk.RegionID != currentRegionID)
        {
            GL.DeleteVertexArray(regionByID[chunk.RegionID].Vao);
            GL.DeleteBuffer(regionByID[chunk.RegionID].Vbo);
            regionByID.Remove(chunk.RegionID);
        }
        OutputErrors("Voxel Mat DeactivateChunk");
    }

    private unsafe void NewRegion()
    {
        uint vbo;
        uint vao;
        GL.GenBuffers(1, out vbo);
        vao = GL.GenVertexArray();
        regionByID.Add(++currentRegionID, new RegionBuffer(vbo, vao, bufferSize));
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        GL.BindVertexArray(vao);
        CubeFaceInstance[] defaults = new CubeFaceInstance[(int)Math.Ceiling((double)bufferSize / CubeFaceInstance.MemorySize)];
        fixed (void* buf = defaults)
        {
            GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(defaults.Length * CubeFaceInstance.MemorySize), buf, BufferUsageARB.DynamicDraw);
        }
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, CubeFaceInstance.MemorySize, (void*)0);
        GL.VertexAttribDivisor(0, 1);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribIPointer(1, 1, GLEnum.Int, CubeFaceInstance.MemorySize, (void*)memBlockInstanceTextureOffset);
        GL.VertexAttribDivisor(1, 1);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribIPointer(2, 1, GLEnum.Int, CubeFaceInstance.MemorySize, (void*)memBlockInstanceFaceOffset);
        GL.VertexAttribDivisor(2, 1);
        GL.EnableVertexAttribArray(3);
        GL.VertexAttribPointer(3, 3, GLEnum.Float, false, CubeFaceInstance.MemorySize, (void*)memBlockInstanceTintOffset);
        GL.VertexAttribDivisor(3, 1);
        GL.BindVertexArray(0);
        OutputErrors("Voxel Mat Creating Region");
    }

    /// <summary>Executes the rendering pass for all registered chunks that pass the occlusion test.</summary>
    /// <param name="projectionMatrix">The current perspective projection matrix.</param>
    /// <param name="viewMatrix">The current camera view matrix.</param>
    public unsafe void Render(Matrix4x4 projectionMatrix, Matrix4x4 viewMatrix)
    {
        // Render
        GL.UseProgram(shaderProgram);
        GL.UniformMatrix4(projectionLocation, 1, false, (float*)&projectionMatrix);
        GL.UniformMatrix4(viewLocation, 1, false, (float*)&viewMatrix);
        MatrixPlanes.Plane[] planes = MatrixPlanes.ViewFrustum(viewMatrix, projectionMatrix);

        GL.BindTexture(GLEnum.Texture2DArray, tbo);
        foreach (RegionBuffer region in regionByID.Values)
        {
            GL.BindVertexArray(region.Vao);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, region.Vbo);
            foreach (ChunkRenderingData chunk in region.Chunks.Select(p => chunkByPos[p]))
            {
                if (!MatrixPlanes.IsBoxInFrustum(planes, chunk.Position, chunk.Position + Vector3.One * chunkLength))
                    continue;
                GL.Uniform3(chunkPosLocation, chunk.Position);
                GL.DrawArraysInstancedBaseInstance(PrimitiveType.Triangles, 0, 6, (uint)chunk.Blocks.Length, (uint)chunk.RegionInstanceIndex);
            }
        }
        OutputErrors("Voxel Mat Render");
    }

    /// <summary>Initializes the voxel engine by compiling shaders, allocating GPU buffers, and building the texture array.</summary>
    /// <param name="openGL">The GL interface for executing commands.</param>
    /// <param name="GLSLScriptsPath">The directory path containing the .glsl shader files.</param>
    /// <param name="chunkLength">The width/height/depth of a single chunk in blocks.</param>
    /// <param name="vramBufferRegionSize">Vram batch size in bytes to reserve.</param>
    /// <param name="cameraNearPlane">The distance to the camera's near clipping plane.</param>
    /// <param name="imageByTextureID">A dictionary containing the raw pixel data for each texture.</param>
    /// <param name="errorAction">An optional delegate for handling error messages.</param>
    /// <param name="logAction">An optional delegate for handling shader compilation logs.</param>
    public unsafe Shader
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
        string vertexShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "Vertex.glsl"));
        uint vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderCode);
        GL.CompileShader(vertexShader);
        string fragmentShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "Fragment.glsl"));
        uint fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderCode);
        GL.CompileShader(fragmentShader);
        shaderProgram = GL.CreateProgram();
        GL.AttachShader(shaderProgram, vertexShader);
        GL.AttachShader(shaderProgram, fragmentShader);
        GL.LinkProgram(shaderProgram);
        // Verify
        GL.GetProgram(shaderProgram, GLEnum.LinkStatus, out int success);
        if (success == 0)
            OutputError?.Invoke("Program link failed: " + GL.GetProgramInfoLog(shaderProgram));
        // Clean up
        GL.DetachShader(shaderProgram, vertexShader);
        GL.DetachShader(shaderProgram, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        GL.UseProgram(shaderProgram);
        // Assing shader variables
        projectionLocation = GL.GetUniformLocation(shaderProgram, "projection");
        viewLocation = GL.GetUniformLocation(shaderProgram, "view");
        this.chunkLength = chunkLength;
        chunkVolume = chunkLength * chunkLength * chunkLength;
        chunkPosLocation = GL.GetUniformLocation(shaderProgram, "chunkPos");
        // Region Buffers
        int maxSSBOSize = GL.GetInteger(GLEnum.MaxShaderStorageBlockSize);
        if (vramBufferRegionSize > maxSSBOSize)
            throw new Exception($"vramBufferRegionSize size exceeds hardware's allowed size of {maxSSBOSize}");
        int chunkVolumeSize = CubeFaceInstance.MemorySize * chunkVolume;
        if (vramBufferRegionSize < chunkVolumeSize)
            throw new Exception($"vramBufferRegionSize size less than a single chunk. Min {chunkVolumeSize}");
        int waste = vramBufferRegionSize % chunkVolumeSize;
        if (waste > 0)
            OutputLogs("Voxel Mat Instantiator", $"vramBufferRegionSize doesn't align with chunk size {chunkVolumeSize} and wastes {waste} bytes.");
        bufferSize = (uint)vramBufferRegionSize;
        memBlockInstanceTextureOffset = Marshal.OffsetOf<CubeFaceInstance>(nameof(CubeFaceInstance.texture));
        memBlockInstanceTintOffset = Marshal.OffsetOf<CubeFaceInstance>(nameof(CubeFaceInstance.tint));
        memBlockInstanceFaceOffset = Marshal.OffsetOf<CubeFaceInstance>(nameof(CubeFaceInstance.face));
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
        // Shapes
        Vertex[] block = CubeMesh.CreateShapeTris();
        uint shapesBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, shapesBuffer);
        fixed (void* buf = block)
        {
            GL.BufferData(BufferTargetARB.ShaderStorageBuffer, (nuint)(block.Length * sizeof(Vertex)), buf, BufferUsageARB.DynamicDraw);
        }
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 3, shapesBuffer);

        OutputLogs("Shader", GL.GetProgramInfoLog(shaderProgram));
        OutputErrors("Voxel Mat Instantiator");
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


    private class RegionBuffer(uint Vbo, uint Vao, uint BufferSize)
    {
        public readonly uint Vbo = Vbo;
        public readonly uint Vao = Vao;
        public int BytePointer = 0;
        public readonly HashSet<Vector3> Chunks = [];

        public bool CanFit(nuint size) =>
            (nuint)BytePointer + size < BufferSize;
    }
}
