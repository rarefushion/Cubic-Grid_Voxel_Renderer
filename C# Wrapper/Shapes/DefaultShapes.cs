
namespace GalensUnified.CubicGrid.Renderer.NET;

public static class DefaultShapes
{

    public static List<Shape> Create() =>
    [
        .. CubeMesh.CreateFaces()
    ];
}