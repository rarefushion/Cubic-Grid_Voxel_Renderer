using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

public unsafe class MeshMaterial
{
    public GL GL;
    public ICamera camera;
    private static uint _shaderProgram;
    private static int _viewLoc;
    private static int _projLoc;
    private static int _samplerLoc;
    private static ConcurrentDictionary<Mesh, Mesh> meshes = [];
    
    private static readonly string _vertexShaderSource = @"
        #version 330 core

        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec3 aColor;
        layout (location = 2) in vec2 aUV;

        out vec3 vColor;
        out vec2 vUV;

        uniform mat4 view;
        uniform mat4 projection;

        void main()
        {
            vColor = aColor;
            vUV = aUV;
            gl_Position = projection * view * vec4(aPos, 1.0);
        }
    ";

    private static readonly string _fragmentShaderSource = @"
        #version 330 core

        uniform sampler2D uTexture;

        in vec3 vColor;
        in vec2 vUV;
        out vec4 FragColor;

        void main()
        {
            FragColor = texture(uTexture, vUV) * vec4(vColor, 1.0);
        }
    ";

    public Mesh CreateMesh(Vertex[] vertices, uint[] indices, Image texture, bool addToDrawPool = true)
    {
        // Vertices
        uint _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);
        uint _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (Vertex* buf = vertices)
        {
            GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(Vertex)), buf, BufferUsageARB.StaticDraw);
        }

        // Indices
        uint ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        fixed (uint* buf = indices)
        {
            GL.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
        }

        // Textures
        uint _tbo = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _tbo);
        GL.TexImage2D
        (
            TextureTarget.Texture2D,
            0,
            InternalFormat.Rgba,
            (uint)texture.Width,
            (uint)texture.Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            texture.Pixels
        );
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);

        // Link vertex attributes
        uint stride = (uint)sizeof(Vertex);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)sizeof(Vector3));
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, (void*)(sizeof(Vector3)*2));
        GL.EnableVertexAttribArray(2);
        GL.BindVertexArray(0);

        // Create Mesh
        Mesh mesh = new Mesh() { Ebo = ebo, Vao = _vao, Vbo = _vbo, Tbo = _tbo, IndexCount = (uint)indices.Length };;
        if (addToDrawPool)
            meshes.TryAdd(mesh, mesh);
        return mesh;
    }

    public void DrawAll(double deltaTime)
    {
        GL.UseProgram(_shaderProgram);

        GL.Uniform1(_samplerLoc, 0);

        // Camera
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(camera.Fov, camera.AspectRatio, camera.NearPlane, camera.FarPlane);
        Matrix4x4 viewMatrix = Matrix4x4.CreateLookAt(camera.Position, camera.Position + camera.Facing, camera.Up);
        GL.UniformMatrix4(_projLoc, 1, false, (float*)&projection);
        GL.UniformMatrix4(_viewLoc, 1, false, (float*)&viewMatrix);

        Mesh[] toRender = [.. meshes.Values];
        foreach (Mesh mesh in toRender)
            DirectDraw(mesh);
    }

    public void DirectDraw(Mesh mesh)
    {
        GL.BindVertexArray(mesh.Vao);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, mesh.Tbo);
        GL.DrawElements(PrimitiveType.Triangles, mesh.IndexCount, DrawElementsType.UnsignedInt, (void*)0);
    }

    public MeshMaterial(ICamera camera)
    {
        this.camera = camera;

        GL = Program.graphics;
        GL.CreateShaders(_vertexShaderSource, _fragmentShaderSource, out _shaderProgram);

        _viewLoc = GL.GetUniformLocation(_shaderProgram, "view");
        _projLoc = GL.GetUniformLocation(_shaderProgram, "projection");
        _samplerLoc = GL.GetUniformLocation(_shaderProgram, "uTexture");

        Program.window.Render += DrawAll;
    }

    public class Mesh
    {
        public uint Vao;
        public uint Vbo;
        public uint Ebo;
        public uint Tbo;
        public uint IndexCount;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Color;
        public Vector2 UV;
    }
}