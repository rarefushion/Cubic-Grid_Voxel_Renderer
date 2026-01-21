using System.Numerics;

public static class ChunkInfo
{
    public const int size = 16;
    public const int mask = size - 1;

    public const int sizeCubed = size * size * size;
    public static Vector3 chunkPosByGlobalPos(Vector3 pos) => 
        new Vector3((float)Math.Floor(pos.X / size), (float)Math.Floor(pos.Y / size), (float)Math.Floor(pos.Z / size)) * size;
    public static int indexByLocalPos(Vector3 pos) => 
        ((int)Math.Floor(pos.Z) * size + (int)Math.Floor(pos.Y)) * size + (int)Math.Floor(pos.X);
    public static int indexByGlobalPos(Vector3 pos) => 
        indexByLocalPos(localPosByGlobalPos(pos));
    public static Vector3 localPosByIndex(int index) => 
        new Vector3(index % size, (index / size) % size, index / (size * size));
    public static Vector3 localPosByGlobalPos(Vector3 pos) => 
        new Vector3((int)Math.Floor(pos.X) & mask, (int)Math.Floor(pos.Y) & mask, (int)Math.Floor(pos.Z) & mask); // bitwise mask faster %
    public static bool posLocal(Vector3 C) => 
        C is { X: >= 0 and < size, Y: >= 0 and < size, Z: >= 0 and < size };
}