using System.Numerics;
using Silk.NET.OpenGL;

public unsafe class VoxelMaterial
{
    public GL GL;
    public uint shaderProgram;
    public ICamera camera;
    int projectionLocation;
    int viewLocation;
    int sizeLocation;
    int chunkPosLocation;

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
        foreach (Chunk chunk in chunks)
        {
            GL.Uniform3(chunkPosLocation, chunk.position);
            GL.BindVertexArray(chunk.Vao);
            GL.DrawArrays(PrimitiveType.Points, 0, (uint)chunk.blocks.Length);
        }
    }

    public VoxelMaterial(ICamera camera, int chunkSize)
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
        sizeLocation = GL.GetUniformLocation(shaderProgram, "size");
        GL.Uniform1(sizeLocation, chunkSize);
        chunkPosLocation = GL.GetUniformLocation(shaderProgram, "uChunkPos");

        Program.window.Render += Render;
        GL.OutputErrors("Voxel Mat Instantiator");
    }

}