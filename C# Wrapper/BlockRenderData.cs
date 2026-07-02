using System.Collections.Frozen;
using GalensUnified.CubicGrid.Core;

namespace GalensUnified.CubicGrid.Renderer.NET;

/// <summary>The rendering information about this block.</summary>
/// <param name="faceBack   ">The texture ID for -Z face.</param>
/// <param name="faceFront  ">The texture ID for +Z face.</param>
/// <param name="faceTop    ">The texture ID for +Y face.</param>
/// <param name="faceBottom ">The texture ID for -Y face.</param>
/// <param name="faceLeft   ">The texture ID for -X face.</param>
/// <param name="faceRight  ">The texture ID for +X face.</param>
public struct BlockRenderData(int faceBack, int faceFront, int faceTop, int faceBottom, int faceLeft, int faceRight)
{
    public int faceBack = faceBack;
    public int faceFront = faceFront;
    public int faceTop = faceTop;
    public int faceBottom = faceBottom;
    public int faceLeft = faceLeft;
    public int faceRight = faceRight;

    /// <summary>Store your block render data here. Index is blockID.</summary>
    public static BlockRenderData[] renderDataByBlock = [];

    /// <summary>Fetch the texture ID by block and face.</summary>
    /// <remarks>Requires <see cref="renderDataByBlock"/> to be set properly to work.</remarks>
    public static int GetTextureID(int block, Direction face) => face switch
    {
        Direction.Back   => renderDataByBlock[block].faceBack,
        Direction.Front  => renderDataByBlock[block].faceFront,
        Direction.Top    => renderDataByBlock[block].faceTop,
        Direction.Bottom => renderDataByBlock[block].faceBottom,
        Direction.Left   => renderDataByBlock[block].faceLeft,
        Direction.Right  => renderDataByBlock[block].faceRight,
        _ => throw new Exception($"Direction({face}) does not exist.")
    };

    /// <summary>Helps with converting texture names into <see cref="BlockRenderData"/>.</summary>
    public class Factory(IEnumerable<string> nameByID)
    {
        public readonly FrozenDictionary<string, int> textureIDByName = nameByID.Select((Name, i) => (Name, i)).ToDictionary().ToFrozenDictionary();

        public BlockRenderData CreateWithNames
        (
            string faceBack,
            string faceFront,
            string faceTop,
            string faceBottom,
            string faceLeft,
            string faceRight
        ) => new
        (
            textureIDByName[faceBack],
            textureIDByName[faceFront],
            textureIDByName[faceTop],
            textureIDByName[faceBottom],
            textureIDByName[faceLeft],
            textureIDByName[faceRight]
        );

        public BlockRenderData CreateWithName(string allFaces)
        {
            int id = textureIDByName[allFaces];
            return new(id, id, id, id, id, id);
        }

        public Factory(TextureLoader.Texture[] textureByID) : this(textureByID.Select(t => t.Name)) { }
    }
}