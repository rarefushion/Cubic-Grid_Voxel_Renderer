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
/// <param name="tintBack   ">The tint color for -Z face.</param>
/// <param name="tintFront  ">The tint color for +Z face.</param>
/// <param name="tintTop    ">The tint color for +Y face.</param>
/// <param name="tintBottom ">The tint color for -Y face.</param>
/// <param name="tintLeft   ">The tint color for -X face.</param>
/// <param name="tintRight  ">The tint color for +X face.</param>
/// <param name="shape">The shape this block uses.</param>
public struct BlockRenderData
(
    int faceBack, int faceFront, int faceTop, int faceBottom, int faceLeft, int faceRight,
    Vector3 tintBack, Vector3 tintFront, Vector3 tintTop, Vector3 tintBottom, Vector3 tintLeft, Vector3 tintRight,
    IShape shape
)
{
    public int faceBack = faceBack;
    public int faceFront = faceFront;
    public int faceTop = faceTop;
    public int faceBottom = faceBottom;
    public int faceLeft = faceLeft;
    public int faceRight = faceRight;
    public Vector3 tintBack = tintBack;
    public Vector3 tintFront = tintFront;
    public Vector3 tintTop = tintTop;
    public Vector3 tintBottom = tintBottom;
    public Vector3 tintLeft = tintLeft;
    public Vector3 tintRight = tintRight;
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

    /// <summary>Fetch the tint color by block and face.</summary>
    public readonly Vector3 GetTint(Direction face) => face switch
    {
        Direction.Back   => tintBack,
        Direction.Front  => tintFront,
        Direction.Top    => tintTop,
        Direction.Bottom => tintBottom,
        Direction.Left   => tintLeft,
        Direction.Right  => tintRight,
        _ => throw new Exception($"Direction({face}) does not exist.")
    };

    private readonly ShapeInstance[] Instance(Vector3 pos, List<Vector3> faceTints, List<Direction> facesVisible, Direction up, int forward) =>
        shape.Instance(pos, this, faceTints, facesVisible, up, forward);

    /// <summary>Make a new instance of this block.</summary>
    /// <param name="facesVisible">Which faces are visible, aka the block neighbors that are transparent.</param>
    /// <returns>The shape instances to render.</returns>
    public readonly ShapeInstance[] Instance(Vector3 pos, List<Direction> facesVisible, Direction up, int forward)
    {
        List<Vector3> tints = [];
        foreach (Direction face in facesVisible)
            tints.Add(GetTint(face));
        return Instance(pos, tints, facesVisible, up, forward);
    }

    /// <summary>Make a new instance of this block. Face tints are multiplied with the ones providied.</summary>
    /// <param name="facesVisible">Which faces are visible, aka the block neighbors that are transparent.</param>
    /// <param name="faceTints">
    /// Mix these face's tints with this blocks base colors.
    /// Must match <see cref="facesVisible"/>, e.g <see cref="faceTints"/>[0] tints <see cref="facesVisible"/>[0] ect.</param>
    /// <returns>The shape instances to render.</returns>
    public readonly ShapeInstance[] InstanceMixTints(Vector3 pos, List<Vector3> faceTints,  List<Direction> facesVisible, Direction up, int forward)
    {
        for (int i = 0; i < facesVisible.Count; i++)
            faceTints[i] *= GetTint(facesVisible[i]);
        return Instance(pos, faceTints, facesVisible, up, forward);
    }

    /// <summary>Make a new instance of this block. Face tints are replaced with the ones provided.</summary>
    /// <param name="facesVisible">Which faces are visible, aka the block neighbors that are transparent.</param>
    /// <param name="faceTints">
    /// Ignore and replace this blocks base colors with these face's tints.
    /// Must match <see cref="facesVisible"/>, e.g <see cref="faceTints"/>[0] tints <see cref="facesVisible"/>[0] ect.</param>
    /// <returns>The shape instances to render.</returns>
    public readonly ShapeInstance[] InstanceReplaceTints(Vector3 pos, List<Vector3> faceTints,  List<Direction> facesVisible, Direction up, int forward) =>
        Instance(pos, faceTints, facesVisible, up, forward);

    /// <summary>Helps with converting texture names into <see cref="BlockRenderData"/>.</summary>
    public class Factory(IEnumerable<string> nameByID)
    {
        public readonly FrozenDictionary<string, int> textureIDByName = nameByID.Select((Name, i) => (Name, i)).ToDictionary().ToFrozenDictionary();

        private static readonly Vector3 White = Vector3.One;
#region Create
        private BlockRenderData Create
        (
            string faceBack, string faceFront, string faceTop, string faceBottom, string faceLeft, string faceRight,
            Vector3 tintBack, Vector3 tintFront, Vector3 tintTop, Vector3 tintBottom, Vector3 tintLeft, Vector3 tintRight,
            IShape shape
        ) => new
        (
            textureIDByName[faceBack], textureIDByName[faceFront], textureIDByName[faceTop], textureIDByName[faceBottom], textureIDByName[faceLeft], textureIDByName[faceRight],
            tintBack, tintFront, tintTop, tintBottom, tintLeft, tintRight,
            shape
        );

        /// <summary>
        /// Creates a <see cref="BlockRenderData"/> using the provided information.
        /// Tints default to white Vector3(1, 1, 1).
        /// </summary>
        public BlockRenderData CreateWithNames
        (
            string faceBack, string faceFront, string faceTop, string faceBottom, string faceLeft, string faceRight,
            IShape shape
        ) => Create
        (
            faceBack, faceFront, faceTop, faceBottom, faceLeft, faceRight,
            White, White, White, White, White, White,
            shape
        );

        /// <summary>
        /// Creates a <see cref="BlockRenderData"/> using the provided information.
        /// Tints default to white Vector3(1, 1, 1).
        /// </summary>
        public BlockRenderData CreateWithName(string allFaces, IShape shape) => Create
        (
            allFaces, allFaces, allFaces, allFaces, allFaces, allFaces,
            White, White, White, White, White, White,
            shape
        );

        /// <summary>
        /// Creates a <see cref="BlockRenderData"/> using the provided information.
        /// </summary>
        public BlockRenderData CreateWithNamesAndTints
        (
            string faceBack, string faceFront, string faceTop, string faceBottom, string faceLeft, string faceRight,
            Vector3 tintBack, Vector3 tintFront, Vector3 tintTop, Vector3 tintBottom, Vector3 tintLeft, Vector3 tintRight,
            IShape shape
        ) => Create
        (
            faceBack, faceFront, faceTop, faceBottom, faceLeft, faceRight,
            tintBack, tintFront, tintTop, tintBottom, tintLeft, tintRight,
            shape
        );

        /// <summary>
        /// Creates a <see cref="BlockRenderData"/> using the provided information.
        /// All faces share the same textureID.
        /// </summary>
        public BlockRenderData CreateWithNameAndTints
        (
            string allFaces,
            Vector3 tintBack, Vector3 tintFront, Vector3 tintTop, Vector3 tintBottom, Vector3 tintLeft, Vector3 tintRight,
            IShape shape
        ) => Create
        (
            allFaces, allFaces, allFaces, allFaces, allFaces, allFaces,
            tintBack, tintFront, tintTop, tintBottom, tintLeft, tintRight,
            shape
        );

        /// <summary>
        /// Creates a <see cref="BlockRenderData"/> using the provided information.
        /// All faces share the same color tint.
        /// </summary>
        public BlockRenderData CreateWithNamesAndTint
        (
            string faceBack, string faceFront, string faceTop, string faceBottom, string faceLeft, string faceRight,
            Vector3 allTints,
            IShape shape
        ) => Create
        (
            faceBack, faceFront, faceTop, faceBottom, faceLeft, faceRight,
            allTints, allTints, allTints, allTints, allTints, allTints,
            shape
        );

        /// <summary>
        /// Creates a <see cref="BlockRenderData"/> using the provided information.
        /// All faces share the same textureID.
        /// All faces share the same color tint.
        /// </summary>
        public BlockRenderData CreateWithNameAndTint
        (
            string allFaces,
            Vector3 allTints,
            IShape shape
        ) => Create
        (
            allFaces, allFaces, allFaces, allFaces, allFaces, allFaces,
            allTints, allTints, allTints, allTints, allTints, allTints,
            shape
        );
#endregion

        public Factory(TextureLoader.Texture[] textureByID) : this(textureByID.Select(t => t.Name)) { }
    }
}