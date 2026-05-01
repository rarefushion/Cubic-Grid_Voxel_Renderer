using System.Numerics;
using System.Runtime.InteropServices;
using GalensUnified.CubicGrid.Core;
using Silk.NET.Maths;

namespace GalensUnified.CubicGrid.Renderer.NET;

/// <summary>A block to render.</summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly struct FaceInstance(Vector3 position, int block, float brightness, int face)
{
    public const int MemorySize = 24;
    [FieldOffset(0)]
    public readonly Vector3 position = position;
    [FieldOffset(12)]
    public readonly int block = block;
    [FieldOffset(16)]
    public readonly float brightness = brightness;
    [FieldOffset(20)]
    public readonly int face = face;
}