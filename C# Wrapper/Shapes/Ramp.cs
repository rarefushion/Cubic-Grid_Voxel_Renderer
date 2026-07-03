using System.Numerics;
using GalensUnified.CubicGrid.Core;

using static GalensUnified.CubicGrid.Renderer.NET.Shapes.Cube;

namespace GalensUnified.CubicGrid.Renderer.NET.Shapes;

/// <remarks>Top texture is used, not front.</remarks>
public class Ramp(int cubeBackFaceShapeID, int slopeID, int cubeBottomFaceShapeID, int leftTriangleID, int rightTriangleID) : IShape
{
    public readonly int slopeID = slopeID;
    public readonly int leftTriangleID = leftTriangleID;
    public readonly int rightTriangleID = rightTriangleID;

    public Shape[] Create()
    {
        Vertex[] slopeVerts =
        [
            new(new(0.0f, 1.0f, 0.0f), uvOffsets[quadsOffsetForTris[0]]),
            new(new(0.0f, 0.0f, 1.0f), uvOffsets[quadsOffsetForTris[1]]),
            new(new(1.0f, 1.0f, 0.0f), uvOffsets[quadsOffsetForTris[2]]),
            new(new(1.0f, 1.0f, 0.0f), uvOffsets[quadsOffsetForTris[3]]),
            new(new(0.0f, 0.0f, 1.0f), uvOffsets[quadsOffsetForTris[4]]),
            new(new(1.0f, 0.0f, 1.0f), uvOffsets[quadsOffsetForTris[5]])
        ];
        Vertex[] leftTriangleVerts =
        [
            new(new(0.0f, 0.0f, 1.0f), uvOffsets[quadsOffsetForTris[0]]),
            new(new(0.0f, 1.0f, 0.0f), uvOffsets[quadsOffsetForTris[1]]),
            new(new(0.0f, 0.0f, 0.0f), uvOffsets[quadsOffsetForTris[2]])
        ];
        Vertex[] rightTriangleVerts =
        [
            new(new(1.0f, 0.0f, 0.0f), uvOffsets[quadsOffsetForTris[0]]),
            new(new(1.0f, 1.0f, 0.0f), uvOffsets[quadsOffsetForTris[1]]),
            new(new(1.0f, 0.0f, 1.0f), uvOffsets[quadsOffsetForTris[2]])
        ];
        return [ new Shape(slopeVerts), new Shape(leftTriangleVerts), new Shape(rightTriangleVerts) ];
    }

    public ShapeInstance[] Instance(Vector3 position, BlockRenderData renderData, List<Vector3> faceTints, List<Direction> facesVisible,  Direction up, int forward)
    {
        List<ShapeInstance> toReturn = [];
        bool drawSlope = false;
        Vector3[] tints = [ Vector3.One, Vector3.One, Vector3.One ]; // Top, Left, Right
        for (int i = 0; i < facesVisible.Count; i++)
        {
            if (facesVisible[i] is Direction.Left or Direction.Right or Direction.Top or Direction.Front)
            {
                drawSlope = true;
                Vector3 tint = faceTints[i];
                int tintID =
                      facesVisible[i] is Direction.Top or Direction.Front ? 0
                    : facesVisible[i] is Direction.Left ? 1
                    : 2;
                // Sets the assigned tint.
                tints[tintID] = tint;
                // If face wasn't specifically tinted, assume another's.
                tints[0] = tints[0] == Vector3.One ? tint : tints[0];
                tints[1] = tints[1] == Vector3.One ? tint : tints[1];
                tints[2] = tints[2] == Vector3.One ? tint : tints[2];
            }
            else // Back or Bottom
                toReturn.Add(new
                (
                    position,
                    renderData.GetTextureID(facesVisible[i]),
                    faceTints[i],
                    facesVisible[i] == Direction.Back ? cubeBackFaceShapeID : cubeBottomFaceShapeID,
                    up,
                    forward
                ));
        }
        if (drawSlope)
        {
            toReturn.Add(new(position, renderData.GetTextureID(Direction.Top), tints[0], slopeID, up, forward));
            toReturn.Add(new(position, renderData.GetTextureID(Direction.Left), tints[1], leftTriangleID, up, forward));
            toReturn.Add(new(position, renderData.GetTextureID(Direction.Right), tints[2], rightTriangleID, up, forward));
        }
        return [.. toReturn];
    }
}