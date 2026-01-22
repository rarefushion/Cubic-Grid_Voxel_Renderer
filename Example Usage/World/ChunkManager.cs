using System.Numerics;

public static class ChunkManager
{
    public static Dictionary<Vector3, Chunk> chunkByPos = [];
    public static VoxelMaterial material;

    public static void Load()
    {
        FileInfo[] textures = Directory.CreateDirectory(Path.Combine(Program.assets.FullName, "Textures")).GetFiles();
        material = new VoxelMaterial(Program.mainCam, ChunkInfo.size, textures);
        for (int x = 0; x < 12; x++)
        for (int y = -12; y < 1; y++)
        for (int z = 0; z < 12; z++)
        {
            Chunk chunk = new()
            {
                position = new Vector3(x, y, z) * ChunkInfo.size,
                blocks = new int[ChunkInfo.sizeCubed]
            };
            for (int i = 0; i < ChunkInfo.sizeCubed; i++)
                chunk.blocks[i] = (float)(ChunkInfo.localPosByIndex(i).Y + chunk.position.Y) switch
                {
                    > 0 => (int)Block.Air,
                    0f => (int)Block.Grass,
                    > -5 => (int)Block.Dirt,
                    _ => (int)Block.Stone,
                };
            material.AssignChunkRendering(chunk);
        }
    }
}