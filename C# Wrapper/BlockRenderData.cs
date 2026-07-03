using System.Collections.Frozen;
using System.Numerics;
using GalensUnified.CubicGrid.Core;
using GalensUnified.CubicGrid.Renderer.NET.Shapes;

namespace GalensUnified.CubicGrid.Renderer.NET;

/// <summary>The rendering information about this block.</summary>
/// <param name="faceBack   ">The texture ID for -Z face.</param>
/// <param name="faceFront  ">The texture ID for +Z face.</param>
/// <param name="faceTop    ">The texture ID for +Y face.</param>
/// <param name="faceBottom ">The texture ID for -Y face.</param>
/// <param name="faceLeft   ">The texture ID for -X face.</param>
/// <param name="faceRight  ">The texture ID for +X face.</param>
/// <param name="shape">The shape this block uses.</param>
public struct BlockRenderData(int faceBack, int faceFront, int faceTop, int faceBottom, int faceLeft, int faceRight, IShape shape)
{
    public int faceBack = faceBack;
    public int faceFront = faceFront;
    public int faceTop = faceTop;
    public int faceBottom = faceBottom;
    public int faceLeft = faceLeft;
    public int faceRight = faceRight;
    public IShape shape = shape;

    /// <summary>Store your block render data here. Index is blockID.</summary>
    public static BlockRenderData[] renderDataByBlock = [];

    /// <summary>Fetch the texture ID by block and face.</summary>
    public readonly int GetTextureID(Direction face) => face switch
    {
        Direction.Back   => faceBack,
        Direction.Front  => faceFront,
        Direction.Top    => faceTop,
        Direction.Bottom => faceBottom,
        Direction.Left   => faceLeft,
        Direction.Right  => faceRight,
        _ => throw new Exception($"Direction({face}) does not exist.")
    };

    public readonly ShapeInstance[] Instance(Vector3 pos, List<Vector3> faceTints, List<Direction> facesVisible, Direction up, int forward) =>
        shape.Instance(pos, this, faceTints, facesVisible, up, forward);

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
            string faceRight,
            IShape shape
        ) => new
        (
            textureIDByName[faceBack],
            textureIDByName[faceFront],
            textureIDByName[faceTop],
            textureIDByName[faceBottom],
            textureIDByName[faceLeft],
            textureIDByName[faceRight],
            shape
        );

        public BlockRenderData CreateWithName(string allFaces, IShape shape)
        {
            int id = textureIDByName[allFaces];
            return new(id, id, id, id, id, id, shape);
        }

        public Factory(TextureLoader.Texture[] textureByID) : this(textureByID.Select(t => t.Name)) { }
    }
}