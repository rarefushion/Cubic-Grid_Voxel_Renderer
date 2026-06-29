using System.Numerics;
using GalensUnified.CubicGrid.Core;

namespace GalensUnified.CubicGrid.Renderer.NET;

/// <summary>Handles callbacks for chunk culling, specifically called when a face needs to be created.</summary>
public interface IBlockCullingHandler
{
    /// <summary>Called before <see cref="FaceVisible"/>.</summary>
    void CullBegan();
    /// <summary>Called for every face that is visible.</summary>
    /// <param name="localBlockPosition">The blocks position local to the chunk.</param>
    /// <param name="block">The block id.</param>
    /// <param name="faceNormal">The faces normal direction aka the block side.</param>
    void FaceVisible(Vector3 localBlockPosition, ushort block, Direction faceNormal);
}