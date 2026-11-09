using System.Numerics;
using System.Runtime.InteropServices;
using GalensUnified.CubicGrid.Core;

namespace GalensUnified.CubicGrid.Renderer.NET;

public readonly record struct GridAlignedQuadInstanceAndTints(GridAlignedQuadInstance QuadInstance, Vector3[] Tints);

/// <summary>Instances a grid aligned quad with a unique position and texture.</summary>
/// <param name="posX">The quads grid position on the X axis local to the chunks position.</param>
/// <param name="posY">The quads grid position on the Y axis local to the chunks position.</param>
/// <param name="posZ">The quads grid position on the Z axis local to the chunks position.</param>
/// <param name="texture">The texture to apply to this shape.</param>
/// <param name="faceNormal">What face of the cube this quad instances. AKA the normal direction.</param>
/// <param name="scaleU">
/// When looking at the face how many tiles horizontally should this instance take.
/// Always scales in positive directions from it's position.
/// 0 is 1 - 255 is 256 quads.
/// </param>
/// <param name="scaleV">
/// When looking at the face how many tiles vertically should this instance take.
/// Always scales in positive directions from it's position.
/// 0 is 1 - 255 is 256 quads.
/// </param>
/// <param name="tintIndex">The starting tint index.</param>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly struct GridAlignedQuadInstance(byte posX, byte posY, byte posZ, Direction faceNormal, int texture, byte scaleU, byte scaleV, uint tintIndex)
{
    public const int MemorySize = 12;
    [FieldOffset(0)]
    public readonly byte posX = posX;
    [FieldOffset(1)]
    public readonly byte posY = posY;
    [FieldOffset(2)]
    public readonly byte posZ = posZ;
    [FieldOffset(3)]
    public readonly byte normal = (byte)faceNormal;
    [FieldOffset(4)]
    public readonly ushort texture = (ushort)texture;
    [FieldOffset(6)]
    public readonly byte scaleX = scaleU;
    [FieldOffset(7)]
    public readonly byte scaleY = scaleV;
    [FieldOffset(8)]
    public readonly uint tintIndex = tintIndex;
}