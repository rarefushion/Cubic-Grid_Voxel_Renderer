using System.Numerics;

public static class ChunkInfo
{
    public const int length = 16;
    public const int mask = length - 1;

    public const int volume = length * length * length;
    public static Vector3 chunkPosByGlobalPos(Vector3 pos) => 
        new Vector3((float)Math.Floor(pos.X / length), (float)Math.Floor(pos.Y / length), (float)Math.Floor(pos.Z / length)) * length;
    public static int indexByLocalPos(Vector3 pos) => 
        ((int)Math.Floor(pos.Z) * length + (int)Math.Floor(pos.Y)) * length + (int)Math.Floor(pos.X);
    public static int indexByGlobalPos(Vector3 pos) => 
        indexByLocalPos(localPosByGlobalPos(pos));
    public static Vector3 localPosByIndex(int index) => 
        new Vector3(index % length, (index / length) % length, index / (length * length));
    public static Vector3 localPosByGlobalPos(Vector3 pos) => 
        new Vector3((int)Math.Floor(pos.X) & mask, (int)Math.Floor(pos.Y) & mask, (int)Math.Floor(pos.Z) & mask); // bitwise mask faster %
    public static bool posLocal(Vector3 C) => 
        C is { X: >= 0 and < length, Y: >= 0 and < length, Z: >= 0 and < length };
}