using System.Numerics;
using GalensUnified.CubicGrid.Core;

namespace GalensUnified.CubicGrid.Renderer.NET.Shapes;

public class GridAlignedQuads() : IShape
{
    public readonly int[] shapeIDByFace = new int[6];

    public Shape[] Create(int nextShapeID)
    {
        for (int i = 0; i < 6; i++)
            shapeIDByFace[i] = nextShapeID + i;
        return [ new([]), new([]), new([]), new([]), new([]), new([]) ];
    }

    public BlockRenderInstance Instance(Vector3 position, BlockRenderData renderData, List<Vector3> faceTints, List<Direction> facesVisible, Direction up, int forward)
    {
        GridAlignedQuadInstanceAndTint[] toReturn = new GridAlignedQuadInstanceAndTint[facesVisible.Count];
        for (int i = 0; i < facesVisible.Count; i++)
        {
            GridAlignedQuadInstance quad = new
            (
                (byte)position.X, (byte)position.Y, (byte)position.Z,
                facesVisible[i],
                renderData.GetTextureID(facesVisible[i])
            );
            toReturn[i] = new(quad, faceTints[i]);
        }
        return new([], toReturn);
    }
}