using Microsoft.DotNet.PlatformAbstractions;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

static class Program
{
    public static IWindow window;
    public static IInputContext input;
    public static ICamera mainCam;
    public static DirectoryInfo assets;
    public static GL graphics;

    static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Title = "Cubic-Grid Voxel Rendering Example";
        options.PreferredDepthBufferBits = 32;
        window = Window.Create(options);
        window.Load += LoadOrder;
        window.Run();
    }

    static void LoadOrder()
    {
        input = window.CreateInput();
        input.Keyboards[0].KeyDown += (keboard, key, num) =>  { if (key == Key.Escape) Environment.Exit(0); };
        mainCam = new DebugCamera();
        assets = Directory.CreateDirectory(Path.Combine(ApplicationEnvironment.ApplicationBasePath, "Assets"));
        graphics = GraphicsLibrary.Load();

        ChunkManager.Load();
        CubeGenorator.Load();
    }
}