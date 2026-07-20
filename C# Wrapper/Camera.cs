using System.Numerics;

namespace GalensUnified.CubicGrid.Renderer.NET;

public interface ICamera
{
    public Vector3 Position { get; }
    public Vector3 EurlerAngles { get; }
    public float Fov { get; }
    public float AspectRatio { get; }
    public float NearPlane { get; }
    public float FarPlane { get; }
}

public class Camera(Vector3 Position, Vector3 EurlerAngles, float Fov, float AspectRatio, float NearPlane, float FarPlane) : ICamera
{
    public Vector3 Position { get; set; } = Position;

    public Vector3 EurlerAngles { get; set; } = EurlerAngles;

    public float Fov { get; set; } = Fov;

    public float AspectRatio { get; set; } = AspectRatio;

    public float NearPlane { get; set; } = NearPlane;

    public float FarPlane { get; set; } = FarPlane;
}