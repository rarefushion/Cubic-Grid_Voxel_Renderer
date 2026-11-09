using System.Numerics;
using System.Runtime.InteropServices;
using GalensUnified.CubicGrid.Core;

namespace GalensUnified.CubicGrid.Renderer.NET;

public readonly record struct ShapeInstanceAndTint(ShapeInstance ShapeInstance, Vector3 Tint);

/// <summary>Instances a shape with a unique position, texture and color.</summary>
/// <param name="position">The cubes position local to the chunks position.</param>
/// <param name="texture">The texture to apply to this shape.</param>
/// <param name="shape">The shape id. 0 - 256.</param>
/// <param name="up">What is up for this shape.</param>
/// <param name="forward">Clock-wise rotation perpendicular to up. range 0-3.</param>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly struct ShapeInstance(Vector3 position, int texture, int shape, Direction up, int forward)
{
    public const int MemorySize = 16;
    [FieldOffset(0)]
    public readonly Vector3 position = position;
    [FieldOffset(12)]
    public readonly short texture = (short)texture;
    // in shader code this ushort(2 bytes) shape is the first half of the uint(4 bytes) shapeRotation
    [FieldOffset(14)]
    public readonly byte shape = (byte)shape;
    // in shader code this ushort(2 bytes) rotation is the second half of the uint(4 bytes) shapeRotation
    [FieldOffset(15)]
    public readonly byte rotation = (byte)((int)up * 4 + forward);
}