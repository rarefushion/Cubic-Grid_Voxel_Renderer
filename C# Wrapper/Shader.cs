using System.Collections;
using System.Numerics;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace GalensUnified.CubicGrid.Renderer.NET;

public class Shader
{
    public Dictionary<ushort, BlockRenderData> renderDataByBlock;
    public Action<string>? OutputLog;
    public Action<string>? OutputError;
    public BitArray occludedChunks;
    public Vector3 worldOrigin;
    public Vector3 worldLengthDimensions;
    public Vector3 MinBounds => worldOrigin;
    public Vector3 MaxBounds => worldOrigin + worldLengthDimensions;
    public uint shaderProgram;
    public uint occlusionProgram;

    private readonly int chunkVolume;
    private readonly int worldChunkLength;
    private readonly int chunkTotalCount;
    private readonly int projectionLocation;
    private readonly int viewLocation;
    private readonly int projectionInvereseLocation;
    private readonly int viewInvereseLocation;
    private readonly int chunkPosLocation;
    private readonly int chunkIndexLocation;
    private readonly int maxOcclusionRayStepsLocation;
    private readonly int negOcclusionBoundsLocation;
    private readonly int posOcclusionBoundsLocation;
    private readonly int screenSizeOcclusionLocation;
    private readonly uint tbo;
    private readonly uint chunkShaderStorageBuffer;
    private readonly uint chunksOccludedShaderStorageBuffer;
    private bool updateRequired;

    private readonly GL GL;
    private record ChunkRenderingData(Vector3 Position, int WorldIndex, uint Vao);
    private readonly List<ChunkRenderingData> chunks = [];

    /// <summary>Registers a chunk for rendering, updates its block data in the GPU buffer, and initializes its Vertex Array Object.</summary>
    /// <param name="position">The world-space position of the chunk.</param>
    /// <param name="worldIndex">The unique index used to offset data within the shader storage buffer.</param>
    /// <param name="blocks">The collection of block IDs comprising the chunk.</param>
    public unsafe void RenderChunk(Vector3 position, int worldIndex, Span<ushort> blocks)
    {
        GL.UseProgram(shaderProgram);
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, chunkShaderStorageBuffer);
        fixed (ushort* buf = blocks)
        {
            nuint size = (nuint)(blocks.Length * sizeof(ushort));
            GL.BufferSubData(BufferTargetARB.ShaderStorageBuffer, worldIndex * sizeof(ushort), size, buf);
        }

        uint vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);
        ChunkRenderingData chunk = new(position, worldIndex, vao);
        GL.VertexAttribPointer(0, 1, VertexAttribPointerType.Byte, false, sizeof(byte), (void*)0);
        GL.EnableVertexAttribArray(0);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        Vector3 maxCurrent = worldOrigin + worldLengthDimensions;
        if
        (
            chunk.Position.X > maxCurrent.X || chunk.Position.X < worldOrigin.X ||
            chunk.Position.Y > maxCurrent.Y || chunk.Position.Y < worldOrigin.Y ||
            chunk.Position.Z > maxCurrent.Z || chunk.Position.Z < worldOrigin.Z
        )
            updateRequired = true;
        chunks.Add(chunk);
        OutputErrors("Voxel Mat Creating Chunk");
    }

    private void UpdateBounds()
    {
        Vector3 min = Vector3.Zero, max = Vector3.Zero;
        foreach (ChunkRenderingData chunk in chunks)
        {
            min.X = (chunk.Position.X < min.X) ? chunk.Position.X : min.X;
            min.Y = (chunk.Position.Y < min.Y) ? chunk.Position.Y : min.Y;
            min.Z = (chunk.Position.Z < min.Z) ? chunk.Position.Z : min.Z;
            max.X = (chunk.Position.X > max.X) ? chunk.Position.X : max.X;
            max.Y = (chunk.Position.Y > max.Y) ? chunk.Position.Y : max.Y;
            max.Z = (chunk.Position.Z > max.Z) ? chunk.Position.Z : max.Z;
        }
        Vector3 nextOrigin = min, nextMax = nextOrigin + worldLengthDimensions;
        if (max.X > nextMax.X || max.Y > nextMax.Y || max.Z > nextMax.Z)
            throw new Exception("Chunks are out of world bounds.");
        worldOrigin = nextOrigin;
        updateRequired = false;
    }

    private unsafe void ComputeChunksOccluded(Matrix4x4 projectionMatrix, Matrix4x4 viewMatrix, Vector2 screenSize)
    {
        GL.UseProgram(occlusionProgram);
        Matrix4x4.Invert(projectionMatrix, out Matrix4x4 projectionInverse);
        Matrix4x4.Invert(viewMatrix, out Matrix4x4 viewInverse);
        GL.UniformMatrix4(projectionInvereseLocation, 1, false, (float*)&projectionInverse);
        GL.UniformMatrix4(viewInvereseLocation, 1, false, (float*)&viewInverse);
        int maxDistance = worldChunkLength * 16 * 3; // TMP world diagnal length, change to account for cam pos, rot and fov
        GL.Uniform1(maxOcclusionRayStepsLocation, maxDistance);
        GL.Uniform3(negOcclusionBoundsLocation, worldOrigin);
        GL.Uniform3(posOcclusionBoundsLocation, worldOrigin + worldLengthDimensions);
        // Create Occlusion Buffer
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 0, chunkShaderStorageBuffer);
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, chunksOccludedShaderStorageBuffer);
        int val = 0;
        GL.ClearBufferData(GLEnum.ShaderStorageBuffer, GLEnum.R32i, GLEnum.RedInteger, GLEnum.Int, &val);
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 2, chunksOccludedShaderStorageBuffer);

        GL.Uniform2(screenSizeOcclusionLocation, screenSize);
        // 16 is the layout(local_size) in the shader
        GL.DispatchCompute((uint)Math.Ceiling(screenSize.X / 16), (uint)Math.Ceiling(screenSize.Y / 16), 1);
        GL.MemoryBarrier((uint)GLEnum.ShaderStorageBarrierBit);
        int chunksOccludedSize = (int)MathF.Ceiling((float)chunkTotalCount / 32);
        int[] chunksOccluded = new int[chunksOccludedSize];
        fixed (void* d = chunksOccluded)
        {
            GL.GetBufferSubData(BufferTargetARB.ShaderStorageBuffer, 0, (nuint)(chunksOccludedSize * sizeof(int)), d);
        }
        occludedChunks = new(chunksOccluded);
        OutputErrors("Voxel Mat Occlusion");
    }

    /// <summary>Executes the rendering pass for all registered chunks that pass the occlusion test.</summary>
    /// <param name="projectionMatrix">The current perspective projection matrix.</param>
    /// <param name="viewMatrix">The current camera view matrix.</param>
    /// <param name="screenSize">The dimensions of the viewport for occlusion calculations.</param>
    public unsafe void Render(Matrix4x4 projectionMatrix, Matrix4x4 viewMatrix, Vector2 screenSize)
    {
        if (updateRequired)
            UpdateBounds();
        ComputeChunksOccluded(projectionMatrix, viewMatrix, screenSize);
        // Render
        GL.UseProgram(shaderProgram);
        GL.UniformMatrix4(projectionLocation, 1, false, (float*)&projectionMatrix);
        GL.UniformMatrix4(viewLocation, 1, false, (float*)&viewMatrix);

        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, chunkShaderStorageBuffer);
        GL.BindTexture(GLEnum.Texture2DArray, tbo);
        foreach (ChunkRenderingData chunk in chunks)
        {
            if (!occludedChunks[chunk.WorldIndex / chunkVolume])
                continue;
            GL.Uniform3(chunkPosLocation, chunk.Position);
            GL.Uniform1(chunkIndexLocation, chunk.WorldIndex);
            GL.BindVertexArray(chunk.Vao);
            GL.DrawArrays(PrimitiveType.Points, 0, (uint)chunkVolume);
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
        string geomatryShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "Geomatry.glsl"));
        uint geomatryShader = GL.CreateShader(ShaderType.GeometryShader);
        GL.ShaderSource(geomatryShader, geomatryShaderCode);
        GL.CompileShader(geomatryShader);
        string fragmentShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "Fragment.glsl"));
        uint fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderCode);
        GL.CompileShader(fragmentShader);
        shaderProgram = GL.CreateProgram();
        GL.AttachShader(shaderProgram, vertexShader);
        GL.AttachShader(shaderProgram, geomatryShader);
        GL.AttachShader(shaderProgram, fragmentShader);
        GL.LinkProgram(shaderProgram);
        // Verify
        GL.GetProgram(shaderProgram, GLEnum.LinkStatus, out int success);
        if (success == 0)
            OutputError?.Invoke("Program link failed: " + GL.GetProgramInfoLog(shaderProgram));
        // Clean up
        GL.DetachShader(shaderProgram, vertexShader);
        GL.DetachShader(shaderProgram, geomatryShader);
        GL.DetachShader(shaderProgram, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(geomatryShader);
        GL.DeleteShader(fragmentShader);
        GL.UseProgram(shaderProgram);
        // Assing shader variables
        projectionLocation = GL.GetUniformLocation(shaderProgram, "projection");
        viewLocation = GL.GetUniformLocation(shaderProgram, "view");
        chunkVolume = chunkLength * chunkLength * chunkLength;
        this.worldChunkLength = worldLengthInChunks;
        int worldLength = worldLengthInChunks * chunkLength;
        this.worldLengthDimensions = new (worldLength, worldLength, worldLength);
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

        OutputLogs("Shader", GL.GetProgramInfoLog(shaderProgram));
        // Occlusion Compute
        string occlusionComputeCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "OcclusionCompute.glsl"));
        uint computeShader = GL.CreateShader(GLEnum.ComputeShader);
        GL.ShaderSource(computeShader, occlusionComputeCode);
        GL.CompileShader(computeShader);
        occlusionProgram = GL.CreateProgram(); // This or CreateShader do we need to keep?
        GL.AttachShader(occlusionProgram, computeShader);
        GL.LinkProgram(occlusionProgram);
        GL.UseProgram(occlusionProgram);

        GL.Uniform1(GL.GetUniformLocation(occlusionProgram, "nearClip"), cameraNearPlane);
        screenSizeOcclusionLocation = GL.GetUniformLocation(occlusionProgram, "screenSize");
        GL.Uniform1(GL.GetUniformLocation(occlusionProgram, "chunkLength"), chunkLength);
        GL.Uniform1(GL.GetUniformLocation(occlusionProgram, "chunkVolume"), chunkVolume);
        GL.Uniform1(GL.GetUniformLocation(occlusionProgram, "worldLength"), worldLength);
        GL.Uniform1(GL.GetUniformLocation(occlusionProgram, "worldChunkLength"), worldLengthInChunks);
        projectionInvereseLocation = GL.GetUniformLocation(occlusionProgram, "projectionInverse");
        viewInvereseLocation = GL.GetUniformLocation(occlusionProgram, "viewInverse");
        maxOcclusionRayStepsLocation = GL.GetUniformLocation(occlusionProgram, "maxSteps");
        negOcclusionBoundsLocation = GL.GetUniformLocation(occlusionProgram, "negBounds");
        posOcclusionBoundsLocation = GL.GetUniformLocation(occlusionProgram, "posBounds");
        chunksOccludedShaderStorageBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, chunksOccludedShaderStorageBuffer);
        int chunksOccludedSize = (int)MathF.Ceiling((float)chunkTotalCount / 32);
        int[] chunksOccluded = new int[chunksOccludedSize];
        occludedChunks = new BitArray(chunksOccluded);
        fixed (int* buf = chunksOccluded)
        {
            GL.BufferData(BufferTargetARB.ShaderStorageBuffer, (nuint)(chunksOccludedSize * sizeof(int)), buf, BufferUsageARB.DynamicDraw);
        }
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 2, chunksOccludedShaderStorageBuffer);

        OutputLogs("Occlusion compute", GL.GetShaderInfoLog(computeShader));
        OutputLogs("Occlusion compute", GL.GetProgramInfoLog(occlusionProgram));
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
