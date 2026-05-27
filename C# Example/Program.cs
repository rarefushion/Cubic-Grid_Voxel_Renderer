using System.Collections.Concurrent;
using System.Numerics;
using GalensUnified.CubicGrid.Core;
using GalensUnified.CubicGrid.Renderer.NET;
using Microsoft.DotNet.PlatformAbstractions;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

// ChunkDims can be switched out for other sized chunks.
// Core.ChunkDims are chunks of length 16 (there for volume of 4096).
// Others exist for 8(Core.HalfChunkDims), 32(Core.DoubleChunkDims), 64 and 128.
using ChunkDims = GalensUnified.CubicGrid.Core.ChunkDims;

static class Program
{
    // MSAA allows partial transparency.
    // Disabling limits to cutout transparency.
    const bool MSAATransparency = true;
    const int worldLengthInChunks = 13;
    const float shadowIntensity = 0.3f; // light level of shadows 0-1
    public static readonly float sunMoveTimeInterval = .25f;
    public static readonly float sunRoatateDegrees = 0.125f;
    public static Vector3 sunRotation = new(-0.3f, -0.4f, -0.3f);
    public static double sunTimeSinceMove = 0;
    public static bool cursorVisible = false;
    public static float moveSpeed = 2f;
    public static Vector2 previousMousePosition;

    static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Title = "Cubic-Grid Voxel Rendering Example";
        options.PreferredDepthBufferBits = 32;
        if (MSAATransparency)
            options.Samples = 8;
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
            // Air
            {0, new("Null", "Null", "Null", "Null", "Null", "Null")},
            // Grass
            {1, new("Grass Side", "Grass Side", "Grass", "Dirt", "Grass Side", "Grass Side")},
            // Dirt
            {2, new("Dirt", "Dirt", "Dirt", "Dirt", "Dirt", "Dirt")},
            // Stone
            {3, new("Stone", "Stone", "Stone", "Stone", "Stone", "Stone")},
            // Glass
            {
                4, MSAATransparency
                    ? new("Glass_Shade", "Glass_Shade", "Glass_Shade", "Glass_Shade", "Glass_Shade", "Glass_Shade")
                    : new("Glass_Cutout", "Glass_Cutout", "Glass_Cutout", "Glass_Cutout", "Glass_Cutout", "Glass_Cutout")
            }
        };
        // Culling information for BlockCulling
        Dictionary<ushort, BlockCulling.TransparencyMode> transparancyByBlock = new()
        {
            // Air
            {0, BlockCulling.TransparencyMode.CullOnTransparent},
            // Grass
            {1, BlockCulling.TransparencyMode.Opaque},
            // Dirt
            {2, BlockCulling.TransparencyMode.Opaque},
            // Stone
            {3, BlockCulling.TransparencyMode.Opaque},
            // Glass
            {
                4, MSAATransparency
                    ? BlockCulling.TransparencyMode.RenderOnTransparent
                    : BlockCulling.TransparencyMode.CullOnTransparent
            }
        };
        foreach ((ushort block, BlockCulling.TransparencyMode mode) in transparancyByBlock)
            BlockCulling.transparencyModeByBlock.Add(block, mode);

        // Create Graphics and Shader
        GL graphics = window.CreateOpenGL();
        if (MSAATransparency)
        {
            graphics.Enable(EnableCap.Multisample);
            graphics.Enable(EnableCap.SampleAlphaToCoverage);
        }
        graphics.Enable(EnableCap.DepthTest);
        graphics.DepthFunc(DepthFunction.Less);
        graphics.ClearColor(System.Drawing.Color.CornflowerBlue);
        window.Resize += size => graphics.Viewport(0, 0, (uint)window.FramebufferSize.X, (uint)window.FramebufferSize.Y);
        window.Update += delta => graphics.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        DirectoryInfo assets = Directory.CreateDirectory(Path.Combine(ApplicationEnvironment.ApplicationBasePath, "Assets"));
        // Ambiguous between mine and Silk.NET.OpenGL.Shader :sob:
        GalensUnified.CubicGrid.Renderer.NET.Shader shader = new
        (

            graphics,
            Path.Combine(assets.FullName, "GLSL"),
            ChunkDims.Length,
            new(worldLengthInChunks),
            ChunkDims.Volume * FaceInstance.MemorySize * 32, // ChunkVolume * BlockInstance memory size * 32 chunks, 32 is adjustable.
            renderDataByBlock,
            TextureLoader.LoadImages(Directory.CreateDirectory(Path.Combine(assets.FullName, "Textures")).GetFiles()),
            messageErr => Console.WriteLine(messageErr),
            messageLog => Console.WriteLine(messageLog)
        );
        // Shading Setup
        DirectionalLightingSettings directionalLightingSettings =
            new (sunRotation, true, shadowIntensity, 1 - shadowIntensity, 0, true, shadowIntensity, 1, 1024);
        shader.Shading.SetDirectionalLightingSettings(directionalLightingSettings);

        // Create World
        int worldLength = worldLengthInChunks * ChunkDims.Length;
        Vector3D<int> worldPosition = -Vector3D<int>.One * worldLength / 2;
        worldPosition.Y += ChunkDims.Length * 2;
        shader.Shading.SetWorldOriginPosition(worldPosition);
        CreateWorld(shader, worldPosition, ChunkDims.Length, worldLength);

        window.Render += dt => shader.Render
        (
            CameraMatrices.CreateProjectionMatrix(camFov, camAspectRatio, camNearPlane, camFarPlane),
            CameraMatrices.CreateViewMatrix(camPosition, camRotation.X, camRotation.Y, 0)
        );
        window.Render += dt => SunUpdate(dt, shader);
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
    static void CreateWorld
    (
        GalensUnified.CubicGrid.Renderer.NET.Shader shader,
        Vector3D<int> worldPosition,
        int chunkLength,
        int worldLength
    )
    {
        // Spin up threads
        ThreadBatch threadBatch = new(Environment.ProcessorCount);
        // Find positions to create
        List<Vector3D<int>> toCreate = [];
        for (int chunkZ = worldPosition.Z; chunkZ < worldPosition.Z + worldLength; chunkZ += chunkLength)
        for (int chunkX = worldPosition.X; chunkX < worldPosition.X + worldLength; chunkX += chunkLength)
        for (int chunkY = worldPosition.Y; chunkY < worldPosition.Y + worldLength; chunkY += chunkLength)
            toCreate.Add(new(chunkX, chunkY, chunkZ));
        // Create Chunks
        Task[] tasks = new Task[toCreate.Count];
        int taskIndex = 0;
        ConcurrentDictionary<Vector3, uint[]> lightMaskByPos = [];
        ConcurrentDictionary<Vector3, ushort[]> chunkByPos = [];
        foreach (Vector3D<int> chunkPos in toCreate)
        tasks[taskIndex++] = threadBatch.EnqueueJob(() =>
        {
            ushort[] blocks = new ushort[ChunkDims.Volume];
            uint[] lightMask = new uint[ChunkDims.Volume / 32];
            bool allSame = true;
            for (int blockZ = 0; blockZ < chunkLength; blockZ++)
            for (int blockX = 0; blockX < chunkLength; blockX++)
            for (int blockY = 0; blockY < chunkLength; blockY++)
            {
                Vector3D<int> localPos = new(blockX, blockY, blockZ);
                Vector3D<int> blockPos = localPos + chunkPos;
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
                blocks[i] = (Math.Abs(blockPos.Z) % 96 > 80 && Math.Abs(blockPos.X) % 96 > 80) ? (ushort)0 : blocks[i];
                blocks[i] = (Math.Abs(blockPos.Z) % 96 == 80 && Math.Abs(blockPos.X) % 96 > 80) ? (ushort)4 : blocks[i];
                blocks[i] = (Math.Abs(blockPos.Z) % 96 > 80 && Math.Abs(blockPos.X) % 96 == 0 && blockPos.X != 0) ? (ushort)4 : blocks[i];
                blocks[i] = (Math.Abs(blockPos.Z) % 96 == 0 && Math.Abs(blockPos.X) % 96 > 80 && blockPos.Z != 0) ? (ushort)4 : blocks[i];
                blocks[i] = (Math.Abs(blockPos.Z) % 96 > 80 && Math.Abs(blockPos.X) % 96 == 80) ? (ushort)4 : blocks[i];
                blocks[i] = (Math.Abs(blockPos.Z) == blockPos.Y && Math.Abs(blockPos.X) % 10 > 5) ? (ushort)1 : blocks[i];
                blocks[i] = (Math.Abs(blockPos.X) == blockPos.Y && Math.Abs(blockPos.Z) % 14 > 7) ? (ushort)1 : blocks[i];
                if (blocks[i] != blocks[0])
                    allSame = false;
                if (blocks[i] != 0 && BlockCulling.transparencyModeByBlock[blocks[i]] == BlockCulling.TransparencyMode.Opaque)
                {
                    int uintIndex = i / 32;
                    int bitIndex = i % 32;
                    lightMask[uintIndex] |= 1u << bitIndex;
                }
            }
            if (allSame && blocks[0] == 0)
                return;
            lightMaskByPos.TryAdd((Vector3)chunkPos, lightMask);
            chunkByPos.TryAdd((Vector3)chunkPos, blocks);
        }).ContinueWith(T => { if (T.Exception != null) throw T.Exception; });
        Task.WhenAll(tasks).GetAwaiter().GetResult();
        // Cull faces
        taskIndex = 0;
        tasks = new Task[chunkByPos.Count];
        ConcurrentDictionary<Vector3, FaceInstance[]> chunksToRender = [];
        foreach (KeyValuePair<Vector3, ushort[]> kvp in chunkByPos)
        tasks[taskIndex++] = threadBatch.EnqueueJob(() =>
        {
            ushort[] negZChunk = chunkByPos.TryGetValue(kvp.Key + (BlockCulling.directions[0] * chunkLength), out ushort[]? negZBlocks) ? negZBlocks : [];
            ushort[] posZChunk = chunkByPos.TryGetValue(kvp.Key + (BlockCulling.directions[1] * chunkLength), out ushort[]? posZBlocks) ? posZBlocks : [];
            ushort[] posYChunk = chunkByPos.TryGetValue(kvp.Key + (BlockCulling.directions[2] * chunkLength), out ushort[]? posYBlocks) ? posYBlocks : [];
            ushort[] negYChunk = chunkByPos.TryGetValue(kvp.Key + (BlockCulling.directions[3] * chunkLength), out ushort[]? negYBlocks) ? negYBlocks : [];
            ushort[] negXChunk = chunkByPos.TryGetValue(kvp.Key + (BlockCulling.directions[4] * chunkLength), out ushort[]? negXBlocks) ? negXBlocks : [];
            ushort[] posXChunk = chunkByPos.TryGetValue(kvp.Key + (BlockCulling.directions[5] * chunkLength), out ushort[]? posXBlocks) ? posXBlocks : [];
            FaceInstance[] toRender = BlockCulling.CullChunk(kvp.Value, chunkLength,  negZChunk, posZChunk, posYChunk, negYChunk, negXChunk, posXChunk);
            if (toRender.Length > 0)
                chunksToRender.TryAdd(kvp.Key, toRender);
        }).ContinueWith(T => { if (T.Exception != null) throw T.Exception; });
        Task.WhenAll(tasks).GetAwaiter().GetResult();
        // Assign Shading Masks
        foreach ((Vector3 chunkPos, uint[] mask) in lightMaskByPos)
            shader.Shading.SetChunkMask(chunkPos.Floor(), mask);
        shader.Shading.SetDirectionalLight(sunRotation);
        // Render And Shade
        foreach ((Vector3 chunkPos, FaceInstance[] blocks) in chunksToRender)
        {
            shader.RenderChunk(chunkPos, blocks);
            shader.Shading.DirectionalShadeChunk(chunkPos);
        }
        threadBatch.Dispose();
    }

    public static void SunUpdate(double deltaTime, GalensUnified.CubicGrid.Renderer.NET.Shader shader)
    {
        const double maxDelta = 0.5;
        sunTimeSinceMove += Math.Min(deltaTime, maxDelta);
        while (sunTimeSinceMove >= sunMoveTimeInterval)
        {
            sunTimeSinceMove -= sunMoveTimeInterval;
            sunRotation = Vector3.Transform(sunRotation, Quaternion.CreateFromAxisAngle(Vector3.UnitY, sunRoatateDegrees));
            shader.Shading.SetDirectionalLight(sunRotation);
            foreach (Vector3 chunk in shader.chunkByPos.Keys)
                shader.Shading.DirectionalShadeChunk(chunk);
        }
    }
}