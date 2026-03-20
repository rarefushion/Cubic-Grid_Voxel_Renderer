using System.Numerics;
using System.Runtime.CompilerServices;

namespace GalensUnified.CubicGrid.Renderer.NET;

public static class CameraMatrices
{
    /// <summary>Creates a perspective projection matrix based on a field of view.</summary>
    /// <param name="Fov">Field of view in radians.</param>
    /// <param name="AspectRatio">Aspect ratio, defined as view width divided by height.</param>
    /// <param name="NearPlane">Distance to the near view plane.</param>
    /// <param name="FarPlane">Distance to the far view plane.</param>
    /// <returns>The projection matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateProjectionMatrix(float Fov, float AspectRatio, float NearPlane, float FarPlane) =>
        Matrix4x4.CreatePerspectiveFieldOfView(Fov, AspectRatio, NearPlane, FarPlane);

    /// <summary>Creates a view matrix using a camera position, a forward direction, and an up vector.</summary>
    /// <param name="position">The world position of the camera.</param>
    /// <param name="facing">The direction vector the camera is looking toward.</param>
    /// <param name="up">The upward direction of the world, usually <see cref="Vector3.UnitY"/>.</param>
    /// <returns>The view matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateViewMatrix(Vector3 position, Vector3 facing, Vector3 up) =>
        Matrix4x4.CreateLookAt(position, position + facing, up);

    /// <summary>Creates a view matrix using a camera position and a rotation quaternion.</summary>
    /// <param name="position">The world position of the camera.</param>
    /// <param name="rotation">The orientation of the camera.</param>
    /// <returns>The view matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateViewMatrix(Vector3 position, Quaternion rotation) =>
        CreateViewMatrix(position, Vector3.Transform(-Vector3.UnitZ, rotation), Vector3.Transform(Vector3.UnitY, rotation));

    /// <summary>Creates a view matrix using a camera position and Euler angles.</summary>
    /// <param name="position">The world position of the camera.</param>
    /// <param name="pitch">The pitch angle in radians.</param>
    /// <param name="yaw">The yaw angle in radians.</param>
    /// <param name="roll">The roll angle in radians.</param>
    /// <returns>The view matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateViewMatrix(Vector3 position, float pitch, float yaw, float roll) =>
        CreateViewMatrix(position, Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll));
}