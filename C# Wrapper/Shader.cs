using System.Collections;
using System.Numerics;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace GalensUnified.CubicGrid.Renderer.NET;

public class Shader
{
    public record ChunkRenderingData(Vector3 Position, int WorldIndex, uint Vao);

    public readonly Dictionary<int, ChunkRenderingData> chunkByWorldIndex = [];
    public Dictionary<ushort, BlockRenderData> renderDataByBlock;
    public Action<string>? OutputLog;
    public Action<string>? OutputError;
    public BitArray loadedChunks;
    public uint shaderProgram;

    private readonly int chunkVolume;
    private readonly int worldChunkLength;
    private readonly int chunkTotalCount;
    private readonly int projectionLocation;
    private readonly int viewLocation;
    private readonly int chunkPosLocation;
    private readonly int chunkIndexLocation;
    private readonly uint tbo;
    private readonly uint chunkShaderStorageBuffer;

    private readonly GL GL;

    /// <summary>Registers or replaces a chunk for rendering, updates its block data in the GPU buffer, and initializes its Vertex Array Object.</summary>
    /// <param name="position">The world-space position of the chunk.</param>
    /// <param name="worldIndex">The block index the chunk starts at.</param>
    /// <param name="blocks">The collection of block IDs comprising the chunk.</param>
    public unsafe void RenderChunk(Vector3 position, int worldIndex, Span<ushort> blocks)
    {
        if (chunkByWorldIndex.TryGetValue(worldIndex, out ChunkRenderingData? oldChunk))
        {
            string log =
                $"Log @Voxel Mat Creating Chunk: Chunk at worldIndex'{worldIndex}' already existed. " +
                $"Old position'{chunkByWorldIndex[worldIndex].Position}' New position'{position}'. " +
                $"Deactivating old chunk before rendering the new one.";
            OutputLog?.Invoke(log);
            GL.DeleteVertexArray(oldChunk!.Vao);
        }
        GL.UseProgram(shaderProgram);
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, chunkShaderStorageBuffer);
        fixed (ushort* buf = blocks)
        {
            nuint size = (nuint)(blocks.Length * sizeof(ushort));
            GL.BufferSubData(BufferTargetARB.ShaderStorageBuffer, worldIndex * sizeof(ushort), size, buf);
        }
        NewChunk(position, worldIndex);
    }

    /// <summary>Registers or replaces a chunk for rendering, updates its block data in the GPU buffer, and initializes its Vertex Array Object.</summary>
    /// <param name="position">The world-space position of the chunk.</param>
    /// <param name="worldIndex">The block index the chunk starts at.</param>
    /// <param name="block">The single block that fills the entire chunk.</param>
    public unsafe void FillChunk(Vector3 position, int worldIndex, ushort block)
    {
        if (chunkByWorldIndex.TryGetValue(worldIndex, out ChunkRenderingData? oldChunk))
        {
            string log =
                $"Log @Voxel Mat Creating Chunk: Chunk at worldIndex'{worldIndex}' already existed. " +
                $"Old position'{chunkByWorldIndex[worldIndex].Position}' New position'{position}'. " +
                $"Deactivating old chunk before rendering the new one.";
            OutputLog?.Invoke(log);
            GL.DeleteVertexArray(oldChunk!.Vao);
        }
        GL.UseProgram(shaderProgram);
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, chunkShaderStorageBuffer);
        nuint size = (nuint)(chunkVolume * sizeof(ushort));
        GL.ClearBufferSubData
        (
            BufferTargetARB.ShaderStorageBuffer,
            GLEnum.R16ui,
            worldIndex * sizeof(ushort),
            size,
            GLEnum.RedInteger,
            GLEnum.UnsignedShort,
            &block
        );
        NewChunk(position, worldIndex);
    }

    /// <summary>Deregisters a chunk for rendering, freeing it to be overwritten.</summary>
    /// <param name="worldIndex">The block index the chunk starts at.</param>
    public unsafe void DeactivateChunk(int worldIndex)
    {
        if (chunkByWorldIndex.TryGetValue(worldIndex, out ChunkRenderingData? chunk))
        {
            GL.DeleteVertexArray(chunk!.Vao);
            chunkByWorldIndex.Remove(worldIndex);
        }
        loadedChunks[worldIndex / chunkVolume] = false;
        GL.UseProgram(shaderProgram);
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, chunkShaderStorageBuffer);
        int air = 0;
        nuint size = (nuint)(chunkVolume * sizeof(ushort));
        GL.ClearBufferSubData
        (
            BufferTargetARB.ShaderStorageBuffer,
            GLEnum.R16ui,
            worldIndex * sizeof(ushort),
            size,
            GLEnum.RedInteger,
            GLEnum.UnsignedShort,
            &air
        );
        OutputErrors("Voxel Mat DeactivateChunk");
    }

    private unsafe void NewChunk(Vector3 position, int worldIndex)
    {

        uint vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);
        ChunkRenderingData chunk = new(position, worldIndex, vao);
        GL.VertexAttribPointer(0, 1, VertexAttribPointerType.Byte, false, sizeof(byte), (void*)0);
        GL.EnableVertexAttribArray(0);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        GL.BindVertexArray(0);
        chunkByWorldIndex[worldIndex] = chunk;
        loadedChunks[worldIndex / chunkVolume] = true;
        OutputErrors("Voxel Mat Creating Chunk");
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

        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, chunkShaderStorageBuffer);
        GL.BindTexture(GLEnum.Texture2DArray, tbo);
        foreach (ChunkRenderingData chunk in chunkByWorldIndex.Values)
        {
            if (!loadedChunks[chunk.WorldIndex / chunkVolume])
                continue;
            GL.Uniform3(chunkPosLocation, chunk.Position);
            GL.Uniform1(chunkIndexLocation, chunk.WorldIndex);
            GL.BindVertexArray(chunk.Vao);
            GL.DrawArraysInstanced(GLEnum.Triangles, 0, 36, (uint)chunkVolume);
        }
        OutputErrors("Voxel Mat Render");
    }

    /// <summary>Initializes the voxel engine by compiling shaders, allocating GPU buffers, and building the texture array.</summary>
    /// <param name="openGL">The GL interface for executing commands.</param>
    /// <param name="GLSLScriptsPath">The directory path containing the .glsl shader files.</param>
    /// <param name="chunkLength">The width/height/depth of a single chunk in blocks.</param>
    /// <param name="worldLengthInChunks">The total width of the world measured in chunks.</param>
    /// <param name="cameraNearPlane">The distance to the camera's near clipping plane.</param>
    /// <param name="renderDataByBlock">A dictionary linking block IDs to their specific <c>BlockRenderData</c>.</param>
    /// <param name="imageByName">A dictionary containing the raw pixel data for each texture.</param>
    /// <param name="errorAction">An optional delegate for handling error messages.</param>
    /// <param name="logAction">An optional delegate for handling shader compilation logs.</param>
    public unsafe Shader
    (
        GL openGL,
        string GLSLScriptsPath,
        int chunkLength,
        int worldLengthInChunks,
        float cameraNearPlane,
        Dictionary<ushort, BlockRenderData> renderDataByBlock,
        Dictionary<string, Image> imageByName,
        Action<string>? errorAction = null,
        Action<string>? logAction = null
    )
    {
        GL = openGL;
        this.renderDataByBlock = renderDataByBlock;
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
        chunkVolume = chunkLength * chunkLength * chunkLength;
        this.worldChunkLength = worldLengthInChunks;
        int worldLength = worldLengthInChunks * chunkLength;
        chunkIndexLocation = GL.GetUniformLocation(shaderProgram, "chunkIndex");
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "chunkLength"), chunkLength);
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "chunkVolume"), chunkVolume);
        chunkPosLocation = GL.GetUniformLocation(shaderProgram, "chunkPos");
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "worldLength"), worldLength);
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "worldChunkLength"), worldLengthInChunks);
        // Shader Storage Buffer Object for chunk blocks
        chunkTotalCount = worldLengthInChunks * worldLengthInChunks * worldLengthInChunks;
        int worldTotalSize =  chunkTotalCount * chunkVolume;
        int maxSSBOSize = GL.GetInteger(GLEnum.MaxShaderStorageBlockSize);
        if ((long)worldTotalSize * (long)sizeof(ushort) > maxSSBOSize)
            throw new Exception($"World size is too big for shader storage buffer. Max size: {maxSSBOSize / 1024 / 1024} MB");
        chunkShaderStorageBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, chunkShaderStorageBuffer);
        int[] defaults = new int[worldTotalSize];
        fixed (int* buf = defaults)
        {
            GL.BufferData(BufferTargetARB.ShaderStorageBuffer, (nuint)(worldTotalSize * sizeof(ushort)), buf, BufferUsageARB.DynamicDraw);
        }
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 0, chunkShaderStorageBuffer);

        // Textures
        uint maxX = 0, maxY = 0;
        foreach (Image img in imageByName.Values)
        {
            maxX = (uint)Math.Max(maxX, img.Width);
            maxY = (uint)Math.Max(maxY, img.Height);
        }
        uint tbo;
        GL.GenTextures(1, &tbo);
        GL.BindTexture(GLEnum.Texture2DArray, tbo);
        GL.TexImage3D(GLEnum.Texture2DArray, 0, (int)GLEnum.Rgba, maxX, maxY, (uint)imageByName.Count, 0, GLEnum.Rgba, GLEnum.UnsignedByte, null);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        int imgIndex = 0;
        Dictionary<string, float> textureIndexByName = [];
        foreach (KeyValuePair<string, Image> img in imageByName)
        {
            GL.TexSubImage3D
            (
                GLEnum.Texture2DArray,
                0,
                0,
                0,
                imgIndex,
                (uint)img.Value.Width,
                (uint)img.Value.Height,
                1,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                img.Value.Pixels
            );
            textureIndexByName.Add(img.Key, imgIndex);
            imgIndex++;
        }
        this.tbo = tbo;
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "textureArray"), 0);
        List<float> textureIDs = [];
        foreach (KeyValuePair<ushort, BlockRenderData> blockData in renderDataByBlock)
        {
            if (blockData.Key == 0)
            {
                for (int i = 0; i < 6; i++)
                    textureIDs.Add(3f);
                continue;
            }
            textureIDs.Add(textureIndexByName[blockData.Value.faceBack]);
            textureIDs.Add(textureIndexByName[blockData.Value.faceFront]);
            textureIDs.Add(textureIndexByName[blockData.Value.faceTop]);
            textureIDs.Add(textureIndexByName[blockData.Value.faceBottom]);
            textureIDs.Add(textureIndexByName[blockData.Value.faceLeft]);
            textureIDs.Add(textureIndexByName[blockData.Value.faceRight]);
        }
        uint TextureIDShaderStorageBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, TextureIDShaderStorageBuffer);
        fixed (float* buf = textureIDs.ToArray())
        {
            GL.BufferData(BufferTargetARB.ShaderStorageBuffer, (nuint)(textureIDs.Count * sizeof(float)), buf, BufferUsageARB.DynamicDraw);
        }
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 1, TextureIDShaderStorageBuffer);
        // Shapes
        Vertex[] block = CubeMesh.CreateShapeTris();
        uint shapesBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, shapesBuffer);
        fixed (void* buf = block)
        {
            GL.BufferData(BufferTargetARB.ShaderStorageBuffer, (nuint)(block.Length * sizeof(Vertex)), buf, BufferUsageARB.DynamicDraw);
        }
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 3, shapesBuffer);

        loadedChunks = new(chunkTotalCount);

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
}
