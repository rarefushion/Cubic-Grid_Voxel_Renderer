using System.Numerics;
using Silk.NET.Maths;

public static class ChunkManager
{
    public static ChunkCluster cluster;
    public static VoxelMaterial material;

    public static void Load()
    {
        FileInfo[] textures = Directory.CreateDirectory(Path.Combine(Program.assets.FullName, "Textures")).GetFiles();
        int worldChunkLength = 16, worldLength = worldChunkLength * ChunkInfo.length;
        material = new VoxelMaterial(Program.mainCam, ChunkInfo.length, worldChunkLength, textures);
        Vector3 worldPosition = -Vector3.One * worldLength / 2;
        worldPosition.Y += ChunkInfo.length * 2;
        cluster = new(ChunkInfo.length, Vector3D<int>.One * worldChunkLength);
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
                    > -5 => (int)Block.Dirt,
                    -16 => (int)Block.Air,
                    -31 => (int)Block.Air,
                    -49 => (int)Block.Air,
                    _ => (int)Block.Stone,
                };
                blocks[i] = (blockPos.X == -blockPos.Y) ? (int)Block.Grass : blocks[i];
                blocks[i] = (blockPos.Z == -blockPos.Y) ? (int)Block.Grass : blocks[i];
                blocks[i] = (blockPos.X == blockPos.Y) ? (int)Block.Grass : blocks[i];
                blocks[i] = (blockPos.Z == blockPos.Y) ? (int)Block.Grass : blocks[i];
            }
            material.AssignChunkRendering(chunk, blocks);
        }
    }
}