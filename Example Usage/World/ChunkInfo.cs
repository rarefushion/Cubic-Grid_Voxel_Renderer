using System.Numerics;

public static class ChunkInfo
{
    public const int length = 16;
    public const int mask = length - 1;

    public const int volume = length * length * length;

    public static int IndexByLocalPos(Vector3 pos) =>
        ((int)Math.Floor(pos.Z) * length + (int)Math.Floor(pos.Y)) * length + (int)Math.Floor(pos.X);

    public static int IndexByGlobalPos(Vector3 pos) =>
        IndexByLocalPos(LocalPosByGlobalPos(pos));

    public static Vector3 LocalPosByIndex(int index) =>
        new(index & mask, (index / length) & mask, (index / (length * length)) & mask);

    public static Vector3 LocalPosByGlobalPos(Vector3 pos) =>
        new((int)Math.Floor(pos.X) & mask, (int)Math.Floor(pos.Y) & mask, (int)Math.Floor(pos.Z) & mask);

    public static bool PosLocal(Vector3 C)
    {
        float sum = Vector3.Sum(C);
        return sum >= 0 && sum < volume;
    }
}