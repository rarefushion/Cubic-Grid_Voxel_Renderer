using System.Numerics;
using GalensUnified.CubicGrid.Core;

namespace GalensUnified.CubicGrid.Renderer.NET.Shapes;

public class Cube() : IShape
{
    public readonly int[] shapeIDByFace = new int[6];

    public ShapeInstance[] Instance(Vector3 position, BlockRenderData renderData, List<Vector3> faceTints, List<Direction> facesVisible,  Direction up, int forward)
    {
        ShapeInstance[] toReturn = new ShapeInstance[facesVisible.Count];
        for (int i = 0; i < facesVisible.Count; i++)
            toReturn[i] = new(position, renderData.GetTextureID(facesVisible[i]), faceTints[i], shapeIDByFace[(int)facesVisible[i]], up, forward);
        return toReturn;
    }

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

    public Shape[] Create(int nextShapeID)
    {
        for (int i = 0; i < 6; i++)
            shapeIDByFace[i] = nextShapeID + i;
        return CreateFaces();
    }

    /// <summary>Create any side of a cube.</summary>
    /// <param name="face">The face to create.</param>
    /// <returns>One face of a cube.</returns>
    /// <remarks>If you aren't modifying this face for your shape consider reusing the face shapeID made from the cube.</remarks>
    public static Shape CreateFace(Direction face)
    {
        Shape toReturn = new(new Vertex[6]);
        for (int t = 0; t < 6; t++)
        {
            toReturn.Vertices[t] = new
            (
                vertices[quads[4 * (int)face + quadsOffsetForTris[t]]],
                uvOffsets[quadsOffsetForTris[t]]
            );
        }
        return toReturn;
    }

    /// <summary>Creates all 6 faces to make a cube.</summary>
    public static Shape[] CreateFaces()
    {
        Shape[] toReturn = new Shape[6];
        for (Direction f = 0; f < (Direction)6; f++)
            toReturn[(int)f] = CreateFace(f);
        return [.. toReturn];
    }

    public Model[] GetModels(BlockRenderData renderData)
    {
        return[new
        (
            renderData.faceBack, renderData.faceFront, renderData.faceTop, renderData.faceBottom, renderData.faceLeft, renderData.faceRight,
            shapeIDByFace[0], shapeIDByFace[1], shapeIDByFace[2], shapeIDByFace[3], shapeIDByFace[4], shapeIDByFace[5],
            false, false, false, false, false, false
        )];
    }
}