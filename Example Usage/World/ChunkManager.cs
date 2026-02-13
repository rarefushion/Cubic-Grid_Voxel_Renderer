using System.Numerics;

public static class ChunkManager
{
    public static Dictionary<Vector3, Chunk> chunkByPos = [];
    public static VoxelMaterial material;

    public static void Load()
    {
        FileInfo[] textures = Directory.CreateDirectory(Path.Combine(Program.assets.FullName, "Textures")).GetFiles();
        int worldLength = 12;
        material = new VoxelMaterial(Program.mainCam, ChunkInfo.length, worldLength, textures);
        int index = 0;
        for (int x = -worldLength / 2; x < worldLength / 2; x++)
        for (int y = -worldLength + 2; y < 2; y++)
        for (int z = -worldLength / 2; z < worldLength / 2; z++)
        {
            Chunk chunk = new()
            {
                position = new Vector3(x, y, z) * ChunkInfo.length,
                blocks = new int[ChunkInfo.volume],
                worldIndex = index++
            };
            for (int i = 0; i < ChunkInfo.volume; i++)
            {
                chunk.blocks[i] = (float)(ChunkInfo.localPosByIndex(i).Y + chunk.position.Y) switch
                {
                    > 0 => (int)Block.Air,
                    0f => (int)Block.Grass,
                    > -5 => (int)Block.Dirt,
                    _ => (int)Block.Stone,
                };
                Vector3 blockPos = ChunkInfo.localPosByIndex(i) + chunk.position;
                chunk.blocks[i] = (blockPos.X == -blockPos.Y) ? (int)Block.Grass : chunk.blocks[i];
                chunk.blocks[i] = (blockPos.Z == -blockPos.Y) ? (int)Block.Grass : chunk.blocks[i];
            }
            material.AssignChunkRendering(chunk);
        }
    }
}