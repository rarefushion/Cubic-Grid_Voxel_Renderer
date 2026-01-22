using System.Numerics;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

public unsafe class VoxelMaterial
{
    public GL GL;
    public uint shaderProgram;
    public ICamera camera;
    int projectionLocation;
    int viewLocation;
    int chunkPosLocation;
    uint tbo;

    public List<Chunk> chunks = [];

    public static string GLSLScriptsPath;

    public unsafe void AssignChunkRendering(Chunk chunk)
    {
        GL.UseProgram(shaderProgram);

        uint vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);
        chunk.Vao = vao;
        uint vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        fixed (int* buf = chunk.blocks)
        {
            nuint size = (nuint)(chunk.blocks.Length * sizeof(int));
            GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(chunk.blocks.Length * sizeof(int)), buf, BufferUsageARB.StaticDraw);
            GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, size, buf); 
        }
        uint stride = (uint)sizeof(int);
        GL.VertexAttribPointer(0, 1, VertexAttribPointerType.Int, false, stride, (void*)0); // Block
        GL.EnableVertexAttribArray(0);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        GL.BindVertexArray(0);
        chunks.Add(chunk);
        GL.OutputErrors("Voxel Mat Creating Chunk");
    }

    public void Render(double deltaTime)
    {
        GL.UseProgram(shaderProgram);
        // Camera
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(camera.Fov, camera.AspectRatio, camera.NearPlane, camera.FarPlane);
        Matrix4x4 viewMatrix = Matrix4x4.CreateLookAt(camera.Position, camera.Position + camera.Facing, camera.Up);
        GL.UniformMatrix4(projectionLocation, 1, false, (float*)&projection);
        GL.UniformMatrix4(viewLocation, 1, false, (float*)&viewMatrix);
        
        GL.BindTexture(GLEnum.Texture2DArray, tbo);
        foreach (Chunk chunk in chunks)
        {
            GL.Uniform3(chunkPosLocation, chunk.position);
            GL.BindVertexArray(chunk.Vao);
            GL.DrawArrays(PrimitiveType.Points, 0, (uint)chunk.blocks.Length);
        }
    }

    public VoxelMaterial(ICamera camera, int chunkSize, FileInfo[] textureLocations)
    {
        this.camera = camera;

        GL = Program.graphics;
        GLSLScriptsPath = Path.Combine(Program.assets.FullName, "GLSL");
        string vertexShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "Vertex.glsl"));
        string geomatryShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "Geomatry.glsl"));
        string fragmentShaderCode = File.ReadAllText(Path.Combine(GLSLScriptsPath, "Fragment.glsl"));
        // Console.WriteLine($"Vertex: {vertexShaderCode}\n\nGeomatry: {geomatryShaderCode}\n\nFragment: {fragmentShaderCode}\n\n");
        GL.CreateShaders(vertexShaderCode, geomatryShaderCode, fragmentShaderCode, out shaderProgram);
        GL.UseProgram(shaderProgram);

        projectionLocation = GL.GetUniformLocation(shaderProgram, "projection");
        viewLocation = GL.GetUniformLocation(shaderProgram, "view");
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "size"), (float)chunkSize);
        chunkPosLocation = GL.GetUniformLocation(shaderProgram, "uChunkPos");

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
        GL.Uniform1(GL.GetUniformLocation(shaderProgram, "textureIDs"), textureIDs.ToArray());

        Program.window.Render += Render;
        GL.OutputErrors("Voxel Mat Instantiator");
    }

}