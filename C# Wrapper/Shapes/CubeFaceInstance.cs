using System.Numerics;
using System.Runtime.InteropServices;
using GalensUnified.CubicGrid.Core;
using Silk.NET.Maths;

namespace GalensUnified.CubicGrid.Renderer.NET;

/// <summary>One face of a cube to render.</summary>
/// <param name="position">The cubes position local to the chunks position.</param>
/// <param name="block">The block id.</param>
/// <param name="tint">The color tint for this face.</param>
/// <param name="face">The face this instance will render 0-5. 0:-z, 1:+z, 2:+y, 3:-y, 4:-x, 5:+x.</param>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly struct CubeFaceInstance(Vector3 position, int block, Vector3 tint, int face)
{
    public const int MemorySize = 32;
    [FieldOffset(0)]
    public readonly Vector3 position = position;
    [FieldOffset(12)]
    public readonly int block = block;
    [FieldOffset(16)]
    public readonly int face = face;
    [FieldOffset(20)]
    public readonly Vector3 tint = tint;
}