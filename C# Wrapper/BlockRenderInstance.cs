namespace GalensUnified.CubicGrid.Renderer.NET;

/// <summary> An instance of a block. Broken into smaller pieces often to improve performance.</summary>
/// <param name="shapeInstancesAndTints"> The shapes and their tints to instance. </param>
/// <param name="quadInstancesAndTints"> The grid aligned quads and their tints instance. </param>
public readonly struct BlockRenderInstance
(
    ShapeInstanceAndTint[] shapeInstancesAndTints,
    GridAlignedQuadInstanceAndTint[] quadInstancesAndTints
)
{
    public readonly ShapeInstanceAndTint[] shapeInstancesAndTints = shapeInstancesAndTints;
    public readonly GridAlignedQuadInstanceAndTint[] quadInstancesAndTints = quadInstancesAndTints;
}