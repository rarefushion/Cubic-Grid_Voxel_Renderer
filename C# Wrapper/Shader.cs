using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;
using GalensUnified.Graphics;
using GalensUnified.Graphics.Buffers;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace GalensUnified.CubicGrid.Renderer.NET;

public class Shader
{
    public record ChunkRenderingData(Vector3 Position, ShapeInstance[] Shapes, int RegionInstanceIndex, int RegionID);
    /// <summary>Provides the <see cref="ShapeInstance"/> buffer that can be filled.</summary>
    /// <param name="SSBO">The buffer to fill.</param>
    /// <param name="ByteOffset">The byte index to start at.</param>
    /// <param name="RegionID">An internal reference only used by <see cref="Shader"/>.</param>
    /// <remarks>Must use <paramref name="ByteOffset"/> or chunks will be overwritten. Max fill range is chunk volume.</remarks>
    public record BufferRental(ShaderStorageBufferObject<ShapeInstance> SSBO, int ByteOffset, int RegionID);

    public readonly Dictionary<Vector3, ChunkRenderingData> chunkByPos = [];
    public Action<string>? OutputLog;
    public Action<string>? OutputError;
    public uint shaderProgram;

    private readonly int chunkLength;
    private readonly int chunkVolume;
    private readonly uint verticesPerShape;
    private readonly int projectionLocation;
    private readonly int viewLocation;
    private readonly int chunkPosLocation;
    private readonly uint vao;
    private readonly uint tbo;
    private readonly uint bufferSize;
    private readonly nint memShapeInstanceTextureOffset;
    private readonly nint memShapeInstanceTintOffset;
    private readonly nint memShapeInstanceShapeOffset;
    private readonly nint memRotationInstanceShapeOffset;

    private readonly GL GL;
    private readonly Dictionary<int, RegionBuffer> regionByID = [];
    private int currentRegionID = 0;

    /// <summary>Registers or replaces a chunk for rendering and assignes them to a VBO to render.</summary>
    /// <param name="position">The world-space position of the chunk.</param>
    /// <param name="shapes">The collection of shape instances to render.</param>
    public unsafe void RenderChunk(Vector3 position, ShapeInstance[] shapes)
    {
        GL.UseProgram(shaderProgram);
        nuint size = (nuint)(shapes.Length * ShapeInstance.MemorySize);
        if (!regionByID[currentRegionID].CanFit(size))
            NewRegion();
        int index = regionByID[currentRegionID].BytePointer;
        regionByID[currentRegionID].SSBO.SetSubData(shapes, index, true);
        ChunkRenderingData chunk = new(position, shapes, index / ShapeInstance.MemorySize, currentRegionID);
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
            GL.DeleteBuffer(regionByID[chunk.RegionID].SSBO.Handle);
            regionByID.Remove(chunk.RegionID);
        }
        OutputErrors("Voxel Mat DeactivateChunk");
    }

    /// <summary>Get a buffer to fill with <see cref="ShapeInstance"/>.</summary>
    /// <remarks>When finished call <see cref="ReturnRental"/>. Must limit <see cref="ShapeInstance"/>s added to chunk volume</remarks>
    public BufferRental RentBuffer()
    {
        nuint size = (nuint)(chunkVolume * ShapeInstance.MemorySize);
        if (!regionByID[currentRegionID].CanFit(size))
            NewRegion();
        return new(regionByID[currentRegionID].SSBO, regionByID[currentRegionID].BytePointer, currentRegionID);
    }

    /// <summary>Signal you're finished with a <see cref="BufferRental"/>.</summary>
    /// <param name="fillableBuffer">The <see cref="BufferRental"/> previously obtained.</param>
    /// <param name="bytesWritten">The bytes used. Not including <see cref="BufferRental.ByteOffset"/>.</param>
    /// <param name="chunkPosition">The chunk position just created.</param>
    public void ReturnRental(BufferRental fillableBuffer, int bytesWritten, Vector3 chunkPosition)
    {
        int count = bytesWritten / ShapeInstance.MemorySize;
        ShapeInstance[] fakeShapes = new ShapeInstance[count];
        ChunkRenderingData chunk = new(chunkPosition, fakeShapes, fillableBuffer.ByteOffset / ShapeInstance.MemorySize, fillableBuffer.RegionID);
        regionByID[fillableBuffer.RegionID].BytePointer += bytesWritten;
        regionByID[fillableBuffer.RegionID].Chunks.Add(chunkPosition);
        chunkByPos[chunkPosition] = chunk;
        OutputErrors("Voxel Mat ReturnRental");
    }

    private unsafe void NewRegion()
    {
        ShaderStorageBufferObject<ShapeInstance> SSBO = new(GL, BufferUsageARB.DynamicDraw, 0, bufferSize);
        regionByID.Add(++currentRegionID, new RegionBuffer(SSBO, bufferSize));
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

        GL.BindVertexArray(vao);
        GL.BindTexture(GLEnum.Texture2DArray, tbo);
        foreach (RegionBuffer region in regionByID.Values)
        {
            region.SSBO.BindBase();
            OutputErrors("Voxel Mat Bind");
            foreach (ChunkRenderingData chunk in region.Chunks.Select(p => chunkByPos[p]))
            {
                if (!MatrixPlanes.IsBoxInFrustum(planes, chunk.Position, chunk.Position + Vector3.One * chunkLength))
                    continue;
                GL.Uniform3(chunkPosLocation, chunk.Position);
                GL.DrawArraysInstancedBaseInstance(PrimitiveType.Triangles, 0, verticesPerShape, (uint)chunk.Shapes.Length, (uint)chunk.RegionInstanceIndex);
            }
            OutputErrors("Voxel Mat Chunks");
        }
        OutputErrors("Voxel Mat Render");
    }

    /// <summary>Initializes the voxel engine by compiling shaders, allocating GPU buffers, and building the texture array.</summary>
    /// <param name="openGL">The GL interface for executing commands.</param>
    /// <param name="GLSLScriptsPath">The directory path containing the .glsl shader files.</param>
    /// <param name="chunkLength">The width/height/depth of a single chunk in blocks.</param>
    /// <param name="vramBufferRegionSize">Vram batch size in bytes to reserve.</param>
    /// <param name="cameraNearPlane">The distance to the camera's near clipping plane.</param>
    /// <param name="imageByTextureID">Maps <see cref="Image"/> textures to a specific textureID.</param>
    /// <param name="shapeByShapeID">
    /// Maps <see cref="Shape"/>s to a specific shapeIndex. Defualts to <see cref="DefaultShapes.Create"/>.
    /// Warning! Keep shapes small as every shape will share the same number of <see cref="Vertex"/>s.
    /// </param>
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
        Shape[] shapeByShapeID,
        Action<string>? errorAction = null,
        Action<string>? logAction = null
    )
    {
        GL = openGL;
        OutputError = errorAction;
        OutputLog = logAction;
        //Create Shader
        string vertexShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "Vertex.glsl"));
        Shaders.ShaderScript Vertex = new(vertexShaderCode, ShaderType.VertexShader);
        string fragmentShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "Fragment.glsl"));
        Shaders.ShaderScript Fragment = new(fragmentShaderCode, ShaderType.FragmentShader);
        shaderProgram = Shaders.CreateShaderProgram(GL, [Vertex, Fragment]);
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
        int chunkVolumeSize = ShapeInstance.MemorySize * chunkVolume;
        if (vramBufferRegionSize < chunkVolumeSize)
            throw new Exception($"vramBufferRegionSize size less than a single chunk. Min {chunkVolumeSize}");
        int waste = vramBufferRegionSize % chunkVolumeSize;
        if (waste > 0)
            OutputLogs("Voxel Mat Instantiator", $"vramBufferRegionSize doesn't align with chunk size {chunkVolumeSize} and wastes {waste} bytes.");
        bufferSize = (uint)vramBufferRegionSize;
        memShapeInstanceTextureOffset = Marshal.OffsetOf<ShapeInstance>(nameof(ShapeInstance.texture));
        memShapeInstanceTintOffset = Marshal.OffsetOf<ShapeInstance>(nameof(ShapeInstance.tint));
        memShapeInstanceShapeOffset = Marshal.OffsetOf<ShapeInstance>(nameof(ShapeInstance.shape));
        memRotationInstanceShapeOffset = Marshal.OffsetOf<ShapeInstance>(nameof(ShapeInstance.rotation));
        currentRegionID = -1;
        NewRegion();
        vao = GL.GenVertexArray();

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
        for (int s = 0; s < shapeByShapeID.Length; s++)
            if (shapeByShapeID[s].Vertices.Length > verticesPerShape)
                verticesPerShape = (uint)shapeByShapeID[s].Vertices.Length;
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "verticesPerShape"), (int)verticesPerShape);
        Vertex[] condensedShapes = new Vertex[shapeByShapeID.Length * verticesPerShape];
        for (int s = 0; s < shapeByShapeID.Length; s++)
        for (int v = 0; v < verticesPerShape; v++)
        {
            if (v < shapeByShapeID[s].Vertices.Length)
                condensedShapes[s * verticesPerShape + v] = shapeByShapeID[s].Vertices[v];
            else
                condensedShapes[s * verticesPerShape + v] = new();

        }
        uint shapesBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, shapesBuffer);
        fixed (void* buf = condensedShapes)
        {
            GL.BufferData(BufferTargetARB.ShaderStorageBuffer, (nuint)(condensedShapes.Length * sizeof(Vertex)), buf, BufferUsageARB.DynamicDraw);
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


    private class RegionBuffer(ShaderStorageBufferObject<ShapeInstance> SSBO, uint BufferSize)
    {
        public readonly ShaderStorageBufferObject<ShapeInstance> SSBO = SSBO;
        public int BytePointer = 0;
        public readonly HashSet<Vector3> Chunks = [];

        public bool CanFit(nuint size) =>
            (nuint)BytePointer + size < BufferSize;
    }
}
