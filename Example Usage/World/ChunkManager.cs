using System.Numerics;
using Silk.NET.Maths;
using GalensUnified.CubicGrid.Renderer.NET;

public static class ChunkManager
{
    public static ChunkCluster cluster;
    public static Shader material;

    public static void Load()
    {
        FileInfo[] textures = Directory.CreateDirectory(Path.Combine(Program.assets.FullName, "Textures")).GetFiles();
        int worldLengthInChunks = 48, worldLength = worldLengthInChunks * ChunkInfo.length;
        material = new Shader
        (
            Program.graphics,
            Path.Combine(Program.assets.FullName, "GLSL"),
            ChunkInfo.length,
            worldLengthInChunks,
            Program.mainCam.NearPlane,
            BlockData.AllBlocks.Select(BD => new KeyValuePair<int, string[]>
            (
                (int)BD.block,
                [
                    BD.faceBack,
                    BD.faceFront,
                    BD.faceTop,
                    BD.faceBottom,
                    BD.faceLeft,
                    BD.faceRight,
                ]
            )).ToDictionary(),
            TextureLoader.LoadImages(textures),
            messageErr => Console.WriteLine(messageErr),
            messageLog => Console.WriteLine(messageLog)
        );
        ICamera cam = Program.mainCam;
        Program.window.Render += dt => material.Render
        (
            CameraMatrices.CreateProjectionMatrix(cam.Fov, cam.AspectRatio, cam.NearPlane, cam.FarPlane),
            CameraMatrices.CreateViewMatrix(cam.Position, cam.QuaternionRotation),
            (Vector2)Program.window.Size
        );
        Vector3 worldPosition = -Vector3.One * worldLength / 2;
        worldPosition.Y += ChunkInfo.length * 2;
        cluster = new(ChunkInfo.length, Vector3D<int>.One * worldLengthInChunks);
        for (int z = (int)worldPosition.Z; z < (int)worldPosition.Z + worldLength; z += ChunkInfo.length)
        for (int x = (int)worldPosition.X; x < (int)worldPosition.X + worldLength; x += ChunkInfo.length)
        for (int y = (int)worldPosition.Y; y < (int)worldPosition.Y + worldLength; y += ChunkInfo.length)
        {
            Vector3D<int> chunkPos = new(x, y, z);
            Chunk chunk = new()
            {
                position = chunkPos,
                worldIndex = cluster.IndexByGlobalPos(chunkPos)
            };
            Span<int> blocks = cluster.GetChunkByIndex(chunk.worldIndex);
            for (int i = 0; i < ChunkInfo.volume; i++)
            {
                Vector3 tmp = ChunkInfo.LocalPosByIndex(i); // ChunkInfo Vector3s need updating
                Vector3D<int> blockPos = new Vector3D<int>((int)tmp.X, (int)tmp.Y, (int)tmp.Z) + chunk.position;
                blocks[i] = blockPos.Y switch
                {
                    > 0 => (int)Block.Air,
                    0 => (int)Block.Grass,
                    -2 => (int)Block.Air,
                    > -5 => (int)Block.Dirt,
                    -16 => (int)Block.Air,
                    -31 => (int)Block.Air,
                    -49 => (int)Block.Air,
                    _ => (int)Block.Stone,
                };
                blocks[i] = (Math.Abs(blockPos.Z) == blockPos.Y && Math.Abs(blockPos.X) % 10 > 5) ? (int)Block.Grass : blocks[i];
                blocks[i] = (Math.Abs(blockPos.X) == blockPos.Y && Math.Abs(blockPos.Z) % 14 > 7) ? (int)Block.Grass : blocks[i];
            }
            material.RenderChunk((Vector3)chunk.position, chunk.worldIndex, blocks);
        }
    }
}