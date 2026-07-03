using System.Numerics;
using GalensUnified.CubicGrid.Core;

namespace GalensUnified.CubicGrid.Renderer.NET.Shapes;

public interface IShape
{
    public Shape[] Create();
    public ShapeInstance[] Instance(Vector3 position, BlockRenderData renderData, List<Vector3> faceTints, List<Direction> facesVisible,  Direction up, int forward);
}