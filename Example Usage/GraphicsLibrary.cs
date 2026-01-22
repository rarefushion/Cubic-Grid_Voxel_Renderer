using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using StbImageSharp;

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

    public static Image LoadImage(string path, bool flip = true) =>
        LoadImage(new FileInfo(path), flip);

    public static Image LoadImage(FileInfo path, bool flip = true)
    {
        Image toReturn;
        using (Stream stream = path.OpenRead())
        {
            ImageResult imageResult = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            toReturn = new Image
                    {
                        Width = imageResult.Width,
                        Height = imageResult.Height,
                        // Pin the byte array in memory and get a pointer to the raw pixel data
                        Pixels = (byte*)Marshal.AllocHGlobal(imageResult.Data.Length)
                    };

            // Copy the data to the unmanaged memory
            Marshal.Copy(imageResult.Data, 0, (IntPtr)toReturn.Pixels, imageResult.Data.Length);
        }
        if (flip)
            return FlipImage(toReturn);
        else
            return toReturn;
    }

    public static Image FlipImage(Image image)
    {
        int count = image.Height * image.Width;
        byte[] fliped = new byte[count * 4];
        for (int p = 0; p < count; p++)
        {
            int index = p * 4;
            fliped[fliped.Length - 1 - (index + 0)] = image.Pixels[index + 3];
            fliped[fliped.Length - 1 - (index + 1)] = image.Pixels[index + 2];
            fliped[fliped.Length - 1 - (index + 2)] = image.Pixels[index + 1];
            fliped[fliped.Length - 1 - (index + 3)] = image.Pixels[index + 0];
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