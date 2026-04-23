using System.Numerics;
using System.Runtime.InteropServices;

namespace GalensUnified.CubicGrid.Renderer.NET;

public struct Vertex(Vector3 position, Vector2 uv, int face)
{
    public Vector3 position = position;
    private float _pad0 = 0; // Padding helps C# and GLSL have the same memory size.
    public Vector2 uv = uv;
    public int face = face;
    private float _pad1 = 0;
}