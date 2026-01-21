using System.Numerics;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

public unsafe static class GraphicsLibrary
{
    public static GL Load()
    {
        GL toReturn = Program.window.CreateOpenGL();
        toReturn.Enable(EnableCap.DepthTest);
        toReturn.DepthFunc(DepthFunction.Less);
        toReturn.ClearColor(System.Drawing.Color.CornflowerBlue);
        Program.window.Resize += OnResize;
        Program.window.Update += PrepareFrame;
        return toReturn;
    }

    public static void PrepareFrame(double deltaTime)
    {
        Program.graphics.OutputErrors("GraphicsLibrary.PrepareFrame");
        Program.graphics.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public static void OnResize(Silk.NET.Maths.Vector2D<int> size) =>
        Program.graphics.Viewport(0, 0, (uint)size.X, (uint)size.Y);

    public static void CreateShaders
    (
        this GL graphics,
        string vertexShaderCode,
        string fragmentShaderCode,
        out uint shaderProgram
    )
    {
        
        uint vertexShader = graphics.CreateShader(ShaderType.VertexShader);
        graphics.ShaderSource(vertexShader, vertexShaderCode);
        graphics.CompileShader(vertexShader);

        uint fragmentShader = graphics.CreateShader(ShaderType.FragmentShader);
        graphics.ShaderSource(fragmentShader, fragmentShaderCode);
        graphics.CompileShader(fragmentShader);

        shaderProgram = graphics.CreateProgram();
        graphics.AttachShader(shaderProgram, vertexShader);
        graphics.AttachShader(shaderProgram, fragmentShader);
        graphics.LinkProgram(shaderProgram);


        graphics.GetProgram(shaderProgram, GLEnum.LinkStatus, out int success);
        if (success == 0)
            Console.WriteLine("Program link failed: " + graphics.GetProgramInfoLog(shaderProgram));

        graphics.DetachShader(shaderProgram, vertexShader);
        graphics.DetachShader(shaderProgram, fragmentShader);
        graphics.DeleteShader(vertexShader);
        graphics.DeleteShader(fragmentShader);
    }

    public static void CreateShaders
    (
        this GL graphics,
        string vertexShaderCode,
        string geomatryShaderCode,
        string fragmentShaderCode,
        out uint shaderProgram
    )
    {
        
        uint vertexShader = graphics.CreateShader(ShaderType.VertexShader);
        graphics.ShaderSource(vertexShader, vertexShaderCode);
        graphics.CompileShader(vertexShader);

        uint geomatryShader = graphics.CreateShader(ShaderType.GeometryShader);
        graphics.ShaderSource(geomatryShader, geomatryShaderCode);
        graphics.CompileShader(geomatryShader);

        uint fragmentShader = graphics.CreateShader(ShaderType.FragmentShader);
        graphics.ShaderSource(fragmentShader, fragmentShaderCode);
        graphics.CompileShader(fragmentShader);

        shaderProgram = graphics.CreateProgram();
        graphics.AttachShader(shaderProgram, vertexShader);
        graphics.AttachShader(shaderProgram, geomatryShader);
        graphics.AttachShader(shaderProgram, fragmentShader);
        graphics.LinkProgram(shaderProgram);


        graphics.GetProgram(shaderProgram, GLEnum.LinkStatus, out int success);
        if (success == 0)
            Console.WriteLine("Program link failed: " + graphics.GetProgramInfoLog(shaderProgram));

        graphics.DetachShader(shaderProgram, vertexShader);
        graphics.DetachShader(shaderProgram, geomatryShader);
        graphics.DetachShader(shaderProgram, fragmentShader);
        graphics.DeleteShader(vertexShader);
        graphics.DeleteShader(geomatryShader);
        graphics.DeleteShader(fragmentShader);
    }

    public static Image FlipImage(Image image)
    {
        // Because the bytes are groups of color (4 floats) we have to group them before fliping.
        Vector4[] colors = new Vector4[image.Height * image.Width];
        for (int p = 0; p < colors.Length; p += 4)
            colors[p / 4] = new Vector4(image.Pixels[p], image.Pixels[p + 1], image.Pixels[p + 2], image.Pixels[p + 3]);
        
        byte[] fliped = new byte[colors.Length * 4];
        for (int p = 0; p < colors.Length; p++)
        {
            fliped[fliped.Length - ((p * 4) + 0) - 1] = (byte)colors[p].W;
            fliped[fliped.Length - ((p * 4) + 1) - 1] = (byte)colors[p].Z;
            fliped[fliped.Length - ((p * 4) + 2) - 1] = (byte)colors[p].Y;
            fliped[fliped.Length - ((p * 4) + 3) - 1] = (byte)colors[p].X;
        }

        fixed (byte* ptr = fliped)
        {
            image.Pixels = ptr;
        }
        return image;
    }

    public static void OutputErrors(this GL GL, string location)
    {
        GLEnum err;
        while ((err = GL.GetError()) != GLEnum.NoError)
            Console.WriteLine($"OpenGL Error @{location}: {err}");
    }
}