using System.Numerics;

public static class ChunkManager
{
    public static Dictionary<Vector3, Chunk> chunkByPos = [];
    public static VoxelMaterial material;

    public static void Load()
    {
        material = new VoxelMaterial(Program.mainCam, ChunkInfo.size);
        Chunk chunk = new();
        chunk.blocks = new int[ChunkInfo.sizeCubed];
        int blockCount = 0;
        for (int i = 0; i < ChunkInfo.sizeCubed; i++)
        {
            chunk.blocks[i] = Random.Shared.NextSingle() > 0.5f ? 0 : 1;
            if (chunk.blocks[i] == 1)
                blockCount++;
        }
        material.AssignChunkRendering(chunk);
    }
}