using System.Numerics;
using GalensUnified.CubicGrid.Renderer.NET;
using Microsoft.DotNet.PlatformAbstractions;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

static class Program
{
    const int worldLengthInChunks = 56;
    public static bool cursorVisible = false;
    public static float moveSpeed = 2f;
    public static Vector2 previousMousePosition;

    static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Title = "Cubic-Grid Voxel Rendering Example";
        options.PreferredDepthBufferBits = 32;
        IWindow window = Window.Create(options);
        window.Load += () => Load(window);
        window.Run();
    }

    static void Load(IWindow window)
    {
        // Camera
        Vector3 camPosition = Vector3.One * 1.5f;
        Vector2 camRotation = Vector2.Zero; // Pitch, Yaw
        float mouseSensitivity = 0.0025f;
        float camFov = MathF.PI * (120f / 360f);
        float camAspectRatio = (float)window.Size.X / window.Size.Y;
        float camNearPlane = 0.1f;
        float camFarPlane = 2000f;

        // Inputs
        IInputContext input = window.CreateInput();
        input.Mice[0].Cursor.CursorMode = CursorMode.Raw;
        input.Keyboards[0].KeyDown += (keboard, key, num) =>
        {
            if (key == Key.Escape)
                Environment.Exit(0);
            if (key == Key.E)
            {
                cursorVisible = !cursorVisible;
                input.Mice[0].Cursor.CursorMode = cursorVisible ? CursorMode.Normal : CursorMode.Raw;
            }
        };
        previousMousePosition = input.Mice[0].Position;
        input.Mice[0].MouseMove += (mouse, pos) => camRotation += GetCameraRotationDelta(mouse, pos, mouseSensitivity);
        window.Update += delta => camPosition += GetCameraPositionDelta(delta, input, camRotation.Y);

        // Create Blocks
        // Faces are named by the Assets/Textures file name.
        Dictionary<ushort, BlockRenderData> renderDataByBlock = new()
        {
            {0, new("Null", "Null", "Null", "Null", "Null", "Null")},                          // Air
            {1, new("Grass Side", "Grass Side", "Grass", "Dirt", "Grass Side", "Grass Side")}, // Grass
            {2, new("Dirt", "Dirt", "Dirt", "Dirt", "Dirt", "Dirt")},                          // Dirt
            {3, new("Stone", "Stone", "Stone", "Stone", "Stone", "Stone")}                     // Stone
        };

        // Create Graphics and Shader
        GL graphics = window.CreateOpenGL();
        graphics.Enable(EnableCap.DepthTest);
        graphics.DepthFunc(DepthFunction.Less);
        graphics.ClearColor(System.Drawing.Color.CornflowerBlue);
        window.Resize += size => graphics.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        window.Update += delta => graphics.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        int chunkLength = 16;
        DirectoryInfo assets = Directory.CreateDirectory(Path.Combine(ApplicationEnvironment.ApplicationBasePath, "Assets"));
        // Ambiguous between mine and Silk.NET.OpenGL.Shader :sob:
        GalensUnified.CubicGrid.Renderer.NET.Shader shader = new
        (

            graphics,
            Path.Combine(assets.FullName, "GLSL"),
            chunkLength,
            worldLengthInChunks,
            camNearPlane,
            renderDataByBlock,
            TextureLoader.LoadImages(Directory.CreateDirectory(Path.Combine(assets.FullName, "Textures")).GetFiles()),
            messageErr => Console.WriteLine(messageErr),
            messageLog => Console.WriteLine(messageLog)
        );

        // Create World
        int worldLength = worldLengthInChunks * chunkLength;
        Vector3D<int> worldPosition = -Vector3D<int>.One * worldLength / 2;
        worldPosition.Y += chunkLength * 2;
        CreateWorld(shader, worldPosition, chunkLength, worldLength);

        window.Render += dt => shader.Render
        (
            CameraMatrices.CreateProjectionMatrix(camFov, camAspectRatio, camNearPlane, camFarPlane),
            CameraMatrices.CreateViewMatrix(camPosition, camRotation.X, camRotation.Y, 0),
            (Vector2)window.Size
        );
    }

    /// <summary>Calculates the camera rotation every frame.</summary>
    /// <returns>Final camera rotation.</returns>
    static Vector2 GetCameraRotationDelta(IMouse mouse, Vector2 pos, float sensitivity)
    {
        if (mouse.Cursor.CursorMode != CursorMode.Raw)
            return Vector2.Zero;

        Vector2 delta = pos - previousMousePosition;
        previousMousePosition = pos;

        float Yaw = delta.X * sensitivity;
        float Pitch = delta.Y * sensitivity;

        // clamp pitch to avoid flipping
        float limit = MathF.PI / 2f - 0.01f;
        Pitch = Math.Clamp(Pitch, -limit, limit);
        return new(-Pitch, -Yaw);
    }

    /// <summary>Calculates the distance the camera needs to move every frame.</summary>
    /// <returns>Distance to move the camera.</returns>
    static Vector3 GetCameraPositionDelta(double deltaTime, IInputContext input, float camYaw)
    {
        IKeyboard keyboard = input.Keyboards[0];
        Vector3 dir = new(-MathF.Sin(camYaw), 0, -(float)Math.Cos(camYaw));
        Vector3 toMove = Vector3.Zero;
        if (keyboard.IsKeyPressed(Key.A))
            toMove = new Vector3(-dir.Z, 0, dir.X) * -1;
        else if (keyboard.IsKeyPressed(Key.D))
            toMove = new Vector3(-dir.Z, 0, dir.X) * 1;

        if (keyboard.IsKeyPressed(Key.S))
            toMove += dir * -1;
        else if (keyboard.IsKeyPressed(Key.W))
            toMove += dir * 1;

        if (keyboard.IsKeyPressed(Key.Space))
            toMove.Y = 1;
        else if (keyboard.IsKeyPressed(Key.ShiftLeft))
            toMove.Y = -1;

        float speedMult = input.Mice[0].ScrollWheels[0].Y;
        speedMult = (speedMult > 0) ? 1.25f : (speedMult < 0) ? 0.75f : 0;
        if (speedMult != 0)
            moveSpeed *= speedMult;

        return toMove * (float)deltaTime * moveSpeed;
    }

    /// <summary>Loops through all chunks and their blocks to create the world.</summary>
    static void CreateWorld(GalensUnified.CubicGrid.Renderer.NET.Shader shader, Vector3D<int> worldPosition, int chunkLength, int worldLength)
    {
        int chunkVolume = chunkLength * chunkLength * chunkLength;
        for (int chunkZ = worldPosition.Z; chunkZ < worldPosition.Z + worldLength; chunkZ += chunkLength)
        for (int chunkX = worldPosition.X; chunkX < worldPosition.X + worldLength; chunkX += chunkLength)
        for (int chunkY = worldPosition.Y; chunkY < worldPosition.Y + worldLength; chunkY += chunkLength)
        {
            Vector3D<int> chunkPos = new(chunkX, chunkY, chunkZ);
            // Position relative to worldPosition
            Vector3D<int> localChunkPos = new
            (
                ((chunkPos.X % worldLength) + worldLength) % worldLength,
                ((chunkPos.Y % worldLength) + worldLength) % worldLength,
                ((chunkPos.Z % worldLength) + worldLength) % worldLength
            );
            // The number of chunks in any direction form worldPosition
            Vector3D<int> chunkCoord = new
            (
                (int)MathF.Floor(localChunkPos.X / chunkLength),
                (int)MathF.Floor(localChunkPos.Y / chunkLength),
                (int)MathF.Floor(localChunkPos.Z / chunkLength)
            );
            int worldIndex = ((chunkCoord.Z * worldLengthInChunks + chunkCoord.Y) * worldLengthInChunks + chunkCoord.X) * chunkVolume;
            bool allSameBlock = false;
            ushort[] blocks = new ushort[chunkVolume];
            for (int blockZ = 0; blockZ < chunkLength; blockZ++)
            for (int blockX = 0; blockX < chunkLength; blockX++)
            for (int blockY = 0; blockY < chunkLength; blockY++)
            {
                Vector3D<int> blockPos = new Vector3D<int>(blockX, blockY, blockZ) + chunkPos;
                int i = (blockZ * chunkLength + blockY) * chunkLength + blockX;
                blocks[i] = blockPos.Y switch
                {
                    > 0 => 0,   // Air above 0
                    0 => 1,     // Grass floor
                    -2 => 0,    // Air slice
                    > -5 => 2,  // Dirt between -5 and 0, the soil layer
                    -16 => 0,   // Air slice
                    -31 => 0,   // Air slice
                    -49 => 0,   // Air slice
                    _ => 3,     // Stone default
                };
                blocks[i] = (Math.Abs(blockPos.Z) == blockPos.Y && Math.Abs(blockPos.X) % 10 > 5) ? (ushort)1 : blocks[i];
                blocks[i] = (Math.Abs(blockPos.X) == blockPos.Y && Math.Abs(blockPos.Z) % 14 > 7) ? (ushort)1 : blocks[i];
                if (blocks[i] != blocks[0])
                    allSameBlock = false;
            }
            if (allSameBlock)
                shader.FillChunk((Vector3)chunkPos, worldIndex, blocks[0]);
            else
                shader.RenderChunk((Vector3)chunkPos, worldIndex, blocks);
        }
    }
}