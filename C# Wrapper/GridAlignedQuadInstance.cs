using System.Numerics;
using System.Runtime.InteropServices;
using GalensUnified.CubicGrid.Core;

namespace GalensUnified.CubicGrid.Renderer.NET;

public readonly record struct GridAlignedQuadInstanceAndTint(GridAlignedQuadInstance QuadInstance, Vector3 Tints);

/// <summary>Instances a grid aligned quad with a unique position and texture.</summary>
/// <param name="posX">The quads grid position on the X axis local to the chunks position.</param>
/// <param name="posY">The quads grid position on the Y axis local to the chunks position.</param>
/// <param name="posZ">The quads grid position on the Z axis local to the chunks position.</param>
/// <param name="texture">The texture to apply to this shape.</param>
/// <param name="faceNormal">What face of the cube this quad instances. AKA the normal direction.</param>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly struct GridAlignedQuadInstance(byte posX, byte posY, byte posZ, Direction faceNormal, int texture)
{
    public const int MemorySize = 8;
    [FieldOffset(0)]
    public readonly byte posX = posX;
    [FieldOffset(1)]
    public readonly byte posY = posY;
    [FieldOffset(2)]
    public readonly byte posZ = posZ;
    [FieldOffset(3)]
    public readonly byte normal = (byte)faceNormal;
    [FieldOffset(4)]
    public readonly int texture = texture;
}