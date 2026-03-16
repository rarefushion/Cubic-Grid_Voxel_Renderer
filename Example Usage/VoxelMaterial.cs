using System.Collections;
using System.Numerics;
using Silk.NET.GLFW;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

public unsafe class VoxelMaterial
{
    public GL GL;
    public uint shaderProgram;
    public uint occlusionProgram;
    public ICamera camera;
    public Vector3D<int> worldOrigin;
    public Vector3D<int> worldLengthDimensions;

    int chunkVolume;
    int worldChunkLength;
    int chunkTotalCount;
    int projectionLocation;
    int viewLocation;
    int projectionInvereseLocation;
    int viewInvereseLocation;
    int chunkPosLocation;
    int chunkIndexLocation;
    int maxOcclusionRayStepsLocation;
    int negOcclusionBoundsLocation;
    int posOcclusionBoundsLocation;
    uint tbo;
    uint chunkShaderStorageBuffer;
    uint chunksOccludedShaderStorageBuffer;
    bool updateRequired;

    public List<Chunk> chunks = [];
    public BitArray occludedChunks;

    public static string GLSLScriptsPath;

    public unsafe void AssignChunkRendering(Chunk chunk, Span<int> blocks)
    {
        GL.UseProgram(shaderProgram);
        // Assign chunk blocks to shader storage buffer
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, chunkShaderStorageBuffer);
        fixed (int* buf = blocks)
        {
            nuint size = (nuint)(blocks.Length * sizeof(int));
            GL.BufferSubData(BufferTargetARB.ShaderStorageBuffer, chunk.worldIndex * sizeof(int), size, buf);
        }

        uint vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);
        chunk.Vao = vao;
        GL.VertexAttribPointer(0, 1, VertexAttribPointerType.Byte, false, sizeof(byte), (void*)0);
        GL.EnableVertexAttribArray(0);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        Vector3D<int> maxCurrent = worldOrigin + worldLengthDimensions;
        if
        (
            chunk.position.X > maxCurrent.X || chunk.position.X < worldOrigin.X ||
            chunk.position.Y > maxCurrent.Y || chunk.position.Y < worldOrigin.Y ||
            chunk.position.Z > maxCurrent.Z || chunk.position.Z < worldOrigin.Z
        )
            updateRequired = true;
        // Recalculate worldOrigin
        chunks.Add(chunk);
        GL.OutputErrors("Voxel Mat Creating Chunk");
    }

    public void ComputeChunksOccluded(Matrix4x4 projectionMatrix, Matrix4x4 viewMatrix)
    {
        GL.UseProgram(occlusionProgram);
        Matrix4x4.Invert(projectionMatrix, out Matrix4x4 projectionInverse);
        Matrix4x4.Invert(viewMatrix, out Matrix4x4 viewInverse);
        GL.UniformMatrix4(projectionInvereseLocation, 1, false, (float*)&projectionInverse);
        GL.UniformMatrix4(viewInvereseLocation, 1, false, (float*)&viewInverse);
        int maxDistance = worldChunkLength * 16 * 3; // TMP world diagnal length, change to account for cam pos, rot and fov
        GL.Uniform1(maxOcclusionRayStepsLocation, maxDistance);
        GL.Uniform3(negOcclusionBoundsLocation, (Vector3)(worldOrigin));
        GL.Uniform3(posOcclusionBoundsLocation, (Vector3)(worldOrigin + worldLengthDimensions));
        // Create Occlusion Buffer
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 0, chunkShaderStorageBuffer);
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, chunksOccludedShaderStorageBuffer);
        int val = 0;
        GL.ClearBufferData(GLEnum.ShaderStorageBuffer, GLEnum.R32i, GLEnum.RedInteger, GLEnum.Int, &val);
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 2, chunksOccludedShaderStorageBuffer);

        Vector2D<float> screenSize = Program.window.Size.As<float>();
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
        GL.OutputErrors("Voxel Mat Occlusion");
    }

    private void Update()
    {
        Vector3D<int> min = Vector3D<int>.Zero, max = Vector3D<int>.Zero;
        foreach (Chunk chunk in chunks)
        {
            min.X = (chunk.position.X < min.X) ? chunk.position.X : min.X;
            min.Y = (chunk.position.Y < min.Y) ? chunk.position.Y : min.Y;
            min.Z = (chunk.position.Z < min.Z) ? chunk.position.Z : min.Z;
            max.X = (chunk.position.X > max.X) ? chunk.position.X : max.X;
            max.Y = (chunk.position.Y > max.Y) ? chunk.position.Y : max.Y;
            max.Z = (chunk.position.Z > max.Z) ? chunk.position.Z : max.Z;
        }
        Vector3D<int> nextOrigin = min, nextMax = nextOrigin + worldLengthDimensions;
        if (max.X > nextMax.X || max.Y > nextMax.Y || max.Z > nextMax.Z)
            throw new Exception("Chunks are out of world bounds.");
        worldOrigin = nextOrigin;
        updateRequired = false;
    }

    public void Render(double deltaTime)
    {
        // Camera
        Matrix4x4 projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(camera.Fov, camera.AspectRatio, camera.NearPlane, camera.FarPlane);
        Matrix4x4 viewMatrix = Matrix4x4.CreateLookAt(camera.Position, camera.Position + camera.Facing, camera.Up);

        if (updateRequired)
            Update();
        ComputeChunksOccluded(projectionMatrix, viewMatrix);
        // Render
        GL.UseProgram(shaderProgram);
        GL.UniformMatrix4(projectionLocation, 1, false, (float*)&projectionMatrix);
        GL.UniformMatrix4(viewLocation, 1, false, (float*)&viewMatrix);
        
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, chunkShaderStorageBuffer);
        GL.BindTexture(GLEnum.Texture2DArray, tbo);
        foreach (Chunk chunk in chunks)
        {
            if (!occludedChunks[chunk.worldIndex / chunkVolume])
                continue;
            GL.Uniform3(chunkPosLocation, (Vector3)chunk.position);
            GL.Uniform1(chunkIndexLocation, chunk.worldIndex);
            GL.BindVertexArray(chunk.Vao);
            GL.DrawArrays(PrimitiveType.Points, 0, (uint)chunkVolume);
        }
        GL.OutputErrors("Voxel Mat Render");
    }

    public VoxelMaterial(ICamera camera, int chunkLength, int worldChunkLength, FileInfo[] textureLocations)
    {
        this.camera = camera;

        GL = Program.graphics;
        GLSLScriptsPath = Path.Combine(Program.assets.FullName, "GLSL");
        string vertexShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "Vertex.glsl"));
        string geomatryShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "Geomatry.glsl"));
        string fragmentShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "Fragment.glsl"));
        GL.CreateShaders(vertexShaderCode, geomatryShaderCode, fragmentShaderCode, out shaderProgram);
        GL.UseProgram(shaderProgram);

        projectionLocation = GL.GetUniformLocation(shaderProgram, "projection");
        viewLocation = GL.GetUniformLocation(shaderProgram, "view");
        chunkVolume = chunkLength * chunkLength * chunkLength;
        this.worldChunkLength = worldChunkLength;
        int worldLength = worldChunkLength * chunkLength;
        this.worldLengthDimensions = new (worldLength, worldLength, worldLength);
        chunkIndexLocation = GL.GetUniformLocation(shaderProgram, "chunkIndex");
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "chunkLength"), chunkLength);
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "chunkVolume"), chunkVolume);
        chunkPosLocation = GL.GetUniformLocation(shaderProgram, "chunkPos");
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "worldLength"), worldLength);
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "worldChunkLength"), worldChunkLength);
        // Shader Storage Buffer Object for chunk blocks
        chunkTotalCount = worldChunkLength * worldChunkLength * worldChunkLength;
        int worldTotalSize =  chunkTotalCount * chunkVolume;
        int maxSSBOSize = GL.GetInteger(GLEnum.MaxShaderStorageBlockSize);
        if ((long)worldTotalSize * (long)sizeof(int) > maxSSBOSize)
            throw new Exception($"World size is too big for shader storage buffer. Max size: {maxSSBOSize / 1024 / 1024} MB");
        chunkShaderStorageBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, chunkShaderStorageBuffer);
        int[] defaults = new int[worldTotalSize];
        fixed (int* buf = defaults)
        {
            GL.BufferData(BufferTargetARB.ShaderStorageBuffer, (nuint)(worldTotalSize * sizeof(int)), buf, BufferUsageARB.DynamicDraw);
        }
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 0, chunkShaderStorageBuffer);

        // Textures
        uint maxX = 0, maxY = 0;
        int index = 0;
        List<KeyValuePair<string, Image>> nameWithImage = [];
        foreach (FileInfo texture in textureLocations.OrderBy(di => di.Name))
        {
            Image image = GraphicsLibrary.LoadImage(texture);
            nameWithImage.Add(new(Path.GetFileNameWithoutExtension(texture.Name), image));
            maxX = (uint)Math.Max(maxX, image.Width);
            maxY = (uint)Math.Max(maxY, image.Height);
        }
        uint tbo;
        GL.GenTextures(1, &tbo);
        GL.BindTexture(GLEnum.Texture2DArray, tbo);
        GL.TexImage3D(GLEnum.Texture2DArray, 0, (int)GLEnum.Rgba, maxX, maxY, (uint)nameWithImage.Count, 0, GLEnum.Rgba, GLEnum.UnsignedByte, null);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        for(int i = 0; i < nameWithImage.Count; i++)
        {
            GL.TexSubImage3D
            (
                GLEnum.Texture2DArray,
                0,
                0,
                0,
                i,
                (uint)nameWithImage[i].Value.Width,
                (uint)nameWithImage[i].Value.Height,
                1,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                nameWithImage[i].Value.Pixels
            );
        }
        this.tbo = tbo;
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "textureArray"), 0);

        // Texture IDs
        Dictionary<string, float> textureIndexByName = [];
        for (int i = 0; i < nameWithImage.Count; i++)
            textureIndexByName.Add(nameWithImage[i].Key, i);
        List<float> textureIDs = [];
        foreach (BlockData block in BlockData.AllBlocks)
        {
            if (block.block == Block.Air)
            {
                for (int i = 0; i < 6; i++)
                    textureIDs.Add(3f); // So we get an error whenever we try to index the first texturID
                continue;
            }
            textureIDs.Add(textureIndexByName[block.faceBack]);
            textureIDs.Add(textureIndexByName[block.faceFront]);
            textureIDs.Add(textureIndexByName[block.faceTop]);
            textureIDs.Add(textureIndexByName[block.faceBottom]);
            textureIDs.Add(textureIndexByName[block.faceLeft]);
            textureIDs.Add(textureIndexByName[block.faceRight]);
        }
        uint TextureIDShaderStorageBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, TextureIDShaderStorageBuffer);
        fixed (float* buf = textureIDs.ToArray())
        {
            GL.BufferData(BufferTargetARB.ShaderStorageBuffer, (nuint)(textureIDs.Count * sizeof(float)), buf, BufferUsageARB.DynamicDraw);
        }
        GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 1, TextureIDShaderStorageBuffer);
        Console.WriteLine(GL.GetProgramInfoLog(shaderProgram));
        // Occlusion Compute
        string occlusionComputeCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "OcclusionCompute.glsl"));
        uint computeShader = GL.CreateShader(GLEnum.ComputeShader);
        GL.ShaderSource(computeShader, occlusionComputeCode);
        GL.CompileShader(computeShader);
        occlusionProgram = GL.CreateProgram(); // This or CreateShader do we need to keep?
        GL.AttachShader(occlusionProgram, computeShader);
        GL.LinkProgram(occlusionProgram);
        GL.UseProgram(occlusionProgram);

        GL.Uniform1(GL.GetUniformLocation(occlusionProgram, "nearClip"), camera.NearPlane);
        int screenSize = GL.GetUniformLocation(occlusionProgram, "screenSize");
        GL.Uniform1(GL.GetUniformLocation(occlusionProgram, "chunkLength"), chunkLength);
        GL.Uniform1(GL.GetUniformLocation(occlusionProgram, "chunkVolume"), chunkVolume);
        GL.Uniform1(GL.GetUniformLocation(occlusionProgram, "worldLength"), worldLength);
        GL.Uniform1(GL.GetUniformLocation(occlusionProgram, "worldChunkLength"), worldChunkLength);
        projectionInvereseLocation = GL.GetUniformLocation(occlusionProgram, "projectionInverse");
        viewInvereseLocation = GL.GetUniformLocation(occlusionProgram, "viewInverse");
        maxOcclusionRayStepsLocation = GL.GetUniformLocation(occlusionProgram, "maxSteps");
        negOcclusionBoundsLocation = GL.GetUniformLocation(occlusionProgram, "negBounds");
        posOcclusionBoundsLocation = GL.GetUniformLocation(occlusionProgram, "posBounds");
        GL.Uniform2(screenSize, (Vector2)Program.window.Size.As<float>());
        Program.window.Resize += size => GL.Uniform2(screenSize, (Vector2)size.As<float>());
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
        Console.WriteLine(GL.GetShaderInfoLog(computeShader));
        Console.WriteLine(GL.GetProgramInfoLog(occlusionProgram));

        Program.window.Render += Render;
        GL.OutputErrors("Voxel Mat Instantiator");
    }

}
