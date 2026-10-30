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

    public ShapeInstance[] Instance(Vector3 position, BlockRenderData renderData, List<Vector3> faceTints, List<Direction> facesVisible, Direction up, int forward)
    {
        throw new Exception("GridAlignedQuads are not actually ShapeInstances, these need to be instanced with GridAlignedQuadInstance.");
    }

    public GridAlignedQuadInstance[] InstanceQuads(Vector3 position, BlockRenderData renderData, List<Direction> facesVisible)
    {
        GridAlignedQuadInstance[] toReturn = new GridAlignedQuadInstance[facesVisible.Count];
        for (int i = 0; i < facesVisible.Count; i++)
            toReturn[i] = new((byte)position.X, (byte)position.Y, (byte)position.Z, facesVisible[i], renderData.GetTextureID(facesVisible[i]));
        return toReturn;
    }
}