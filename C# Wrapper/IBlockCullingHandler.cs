using System.Numerics;
using GalensUnified.CubicGrid.Core;

namespace GalensUnified.CubicGrid.Renderer.NET;

/// <summary>Handles callbacks for chunk culling, specifically called when a face needs to be created.</summary>
public interface IBlockCullingHandler
{
    /// <summary>Called before <see cref="FaceVisible"/>.</summary>
    void CullBegan();
    /// <summary>Called if any face is visible of a shape had false value in <see cref="BlockCulling.isFullBlockByBlock"/>.</summary>
    /// <param name="localBlockPosition">The blocks position local to the chunk.</param>
    /// <param name="block">The block id.</param>
    /// <param name="facesVisible">All of the neighbor blocks that were transparent.</param>
    void ShapeVisible(Vector3 localBlockPosition, ushort block, List<Direction> facesVisible);
}