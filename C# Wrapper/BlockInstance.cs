using System.Numerics;
using System.Runtime.InteropServices;

namespace GalensUnified.CubicGrid.Renderer.NET;

/// <summary>A block to render.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct BlockInstance(Vector3 position, int block)
{
    public readonly Vector3 position = position;
    public readonly int block = block;
}