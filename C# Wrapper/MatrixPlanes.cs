using System.Numerics;

namespace GalensUnified.CubicGrid.Renderer.NET;
// AI wrote this. I did some cleanup.
public static class MatrixPlanes
{
    public struct Plane
    {
        public Vector3 Normal;
        public float Distance;
    }

    /// <summary>Creates the view frustum from the (view x projection) matrix.</summary>
    /// <returns>Near, Far, Top, Bottom, Left and Right planes of the <paramref name="view"/> x <paramref name="projection"/> matrix in that order.</returns>
    public static Plane[] ViewFrustum(Matrix4x4 view, Matrix4x4 projection) =>
        ViewFrustum(view * projection);

    /// <summary>Creates the view frustum from the (view x projection) matrix.</summary>
    /// <param name="vp">View projection created from multiplying projection and view matrices. Multiplication order matters.</param>
    /// <returns>Near, Far, Top, Bottom, Left and Right planes of the <paramref name="vp"/> in that order.</returns>
    public static Plane[] ViewFrustum(Matrix4x4 vp) =>
    [
        // Near: Col3 (Assuming 0 to 1 depth range)
        CreatePlane(vp.M13, vp.M23, vp.M33, vp.M43),
        // Far: Col4 - Col3
        CreatePlane(vp.M14 - vp.M13, vp.M24 - vp.M23, vp.M34 - vp.M33, vp.M44 - vp.M43),
        // Top: Col4 - Col2
        CreatePlane(vp.M14 - vp.M12, vp.M24 - vp.M22, vp.M34 - vp.M32, vp.M44 - vp.M42),
        // Bottom: Col4 + Col2
        CreatePlane(vp.M14 + vp.M12, vp.M24 + vp.M22, vp.M34 + vp.M32, vp.M44 + vp.M42),
        // Left: Col4 + Col1
        CreatePlane(vp.M14 + vp.M11, vp.M24 + vp.M21, vp.M34 + vp.M31, vp.M44 + vp.M41),
        // Right: Col4 - Col1
        CreatePlane(vp.M14 - vp.M11, vp.M24 - vp.M21, vp.M34 - vp.M31, vp.M44 - vp.M41),
    ];

    private static Plane CreatePlane(float x, float y, float z, float w)
    {
        Vector3 normal = new(x, y, z);
        float length = normal.Length();
        return new Plane { Normal = normal / length, Distance = w / length };
    }

    public static bool IsBoxInFrustum(Plane[] planes, Vector3 min, Vector3 max)
    {
        for (int i = 0; i < 6; i++)
        {
            Vector3 normal = planes[i].Normal;
            // Find the "positive" corner (most in the direction of the normal)
            Vector3 positive = new
            (
                normal.X >= 0 ? max.X : min.X,
                normal.Y >= 0 ? max.Y : min.Y,
                normal.Z >= 0 ? max.Z : min.Z
            );
            // If the most-positive corner is behind the plane, the box is out
            if (Vector3.Dot(normal, positive) + planes[i].Distance < 0)
                return false;
        }
        return true;
    }
}