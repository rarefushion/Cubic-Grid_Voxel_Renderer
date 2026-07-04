using System.Numerics;
using GalensUnified.CubicGrid.Core;

namespace GalensUnified.CubicGrid.Renderer.NET.Shapes;

public interface IShape
{
    /// <summary>Creates the shapes that can be instanced.</summary>
    /// <param name="nextShapeID">The first available shapeID.</param>
    /// <returns>The new shapes that were created.</returns>
    public Shape[] Create(int nextShapeID);
    public ShapeInstance[] Instance(Vector3 position, BlockRenderData renderData, List<Vector3> faceTints, List<Direction> facesVisible,  Direction up, int forward);
}