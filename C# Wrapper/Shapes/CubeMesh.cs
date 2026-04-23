using System.Numerics;

namespace GalensUnified.CubicGrid.Renderer.NET;

public static class CubeMesh
{
    /// <summary>When making a square out of triangles use this to index <see cref="quads"/> as the corner.</summary>
    public readonly static int[] quadsOffsetForTris = [0, 1, 2, 2, 1, 3];
    /// <summary>A 2D flattened array for indexing <see cref="vertices"/>. Index this at 4 * face + corner.</summary>
    public static readonly int[] quads = [0, 3, 1, 2, 5, 6, 4, 7, 3, 7, 2, 6, 1, 5, 0, 4, 4, 7, 0, 3, 1, 2, 5, 6];
    /// <summary>Bottom left, counter clock wise, -z corners then +z corners.</summary>
    public static readonly Vector3[] vertices =
    [
        new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f)
    ];
    public static readonly Vector2[] uvOffsets =
    [
        new Vector2(0.0f, 0.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 1.0f)
    ];

    /// <summary>Creates a cube out of triangles.</summary>
    public static Vertex[] CreateShapeTris()
    {
        Vertex[] toReturn = new Vertex[36];
        for (int f = 0; f < 6; f++)
        for (int t = 0; t < 6; t++)
        {
            Vertex vert = new
            (
                vertices[quads[4 * f + quadsOffsetForTris[t]]],
                uvOffsets[quadsOffsetForTris[t]],
                f
            );
            toReturn[6 * f + t] = vert;
        }
        return toReturn;
    }
}