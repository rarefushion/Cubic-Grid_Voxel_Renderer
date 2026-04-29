using System.Numerics;
using System.Runtime.InteropServices;

namespace GalensUnified.CubicGrid.Renderer.NET;

/// <summary>A block to render.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct BlockInstance(Vector3 position, int block, float[] faceLights)
{
    public const int MemorySize = 40;
    public readonly Vector3 position = position;
    public readonly int block = block;
    public readonly Vector3 faceLights1 = new(faceLights[0], faceLights[1], faceLights[2]);
    public readonly Vector3 faceLights2 = new(faceLights[3], faceLights[4], faceLights[5]);
    public float[] FaceLights => [faceLights1.X, faceLights1.Y, faceLights1.Z, faceLights2.X, faceLights2.Y, faceLights2.Z];
}