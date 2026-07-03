using System.Numerics;
using System.Runtime.InteropServices;
using GalensUnified.CubicGrid.Core;

namespace GalensUnified.CubicGrid.Renderer.NET;

/// <summary>Instances a shape with a unique position, texture and color.</summary>
/// <param name="position">The cubes position local to the chunks position.</param>
/// <param name="texture">The texture to apply to this shape.</param>
/// <param name="tint">The color to tint the texture.</param>
/// <param name="shape">The shape id.</param>
/// <param name="up">What is up for this shape.</param>
/// <param name="forward">Clock-wise rotation perpendicular to up. range 0-3.</param>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly struct ShapeInstance(Vector3 position, int texture, Vector3 tint, int shape, Direction up, int forward)
{
    public const int MemorySize = 32;
    [FieldOffset(0)]
    public readonly Vector3 position = position;
    [FieldOffset(12)]
    public readonly int texture = texture;
    [FieldOffset(16)]
    public readonly ushort shape = (ushort)shape;
    [FieldOffset(18)]
    public readonly ushort rotation = (ushort)((int)up * 4 + forward);
    [FieldOffset(20)]
    public readonly Vector3 tint = tint;
}