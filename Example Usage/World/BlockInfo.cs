using System.Numerics;

public static class BlockInfo
{
    public readonly static int[] vertIndexForTris = [0, 1, 2, 2, 1, 3];
    public readonly static int[] quads = [0, 3, 1, 2, 5, 6, 4, 7, 3, 7, 2, 6, 1, 5, 0, 4, 4, 7, 0, 3, 1, 2, 5, 6]; //This is a 2D array just need to index 4 * face
    public readonly static Vector3[] blockVertices =
    [
        Vector3.Zero, new(1.0f, 0.0f, 0.0f), new(1.0f, 1.0f, 0.0f), new(0.0f, 1.0f, 0.0f),
        new(0.0f, 0.0f, 1.0f), new(1.0f, 0.0f, 1.0f), new(1.0f, 1.0f, 1.0f), new(0.0f, 1.0f, 1.0f)
    ];

    public readonly static Vector3[] directions =
        [ new(0f, 0f, -1), new(0f, 0f, 1), new(0f, 1f, 0f), new(0f, -1f, 0f), new(-1f, 0f, 0f), new(1f, 0f, 0f) ];
    public readonly static Vector2[] uvOffsets =
        [ Vector2.Zero, Vector2.UnitY, Vector2.UnitX, Vector2.One ];
}