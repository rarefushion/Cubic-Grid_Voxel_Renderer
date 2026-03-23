namespace GalensUnified.CubicGrid.Renderer.NET;

/// <summary>
/// The rendering information about this block.
/// </summary>
/// <param name="faceBack">The image name the renderer will use for -Z face.</param>
/// <param name="faceFront">The image name the renderer will use for +Z face.</param>
/// <param name="faceTop">The image name the renderer will use for +Y face.</param>
/// <param name="faceBottom">The image name the renderer will use for -Y face.</param>
/// <param name="faceLeft">The image name the renderer will use for -X face.</param>
/// <param name="faceRight">The image name the renderer will use for +X face.</param>
public struct BlockRenderData(string faceBack, string faceFront, string faceTop, string faceBottom, string faceLeft, string faceRight)
{
    public string faceBack = faceBack;
    public string faceFront = faceFront;
    public string faceTop = faceTop;
    public string faceBottom = faceBottom;
    public string faceLeft = faceLeft;
    public string faceRight = faceRight;
}