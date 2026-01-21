using System.Numerics;

public interface ICamera
{
    public Vector3 Position { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float Roll { get; set; }
    public Quaternion QuaternionRotation => Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, Roll);
    public Vector3 Facing { get; }
    public Vector3 Up { get; }
    public float Fov { get; set; }
    public float AspectRatio { get; set; }
    public float NearPlane { get; set; }
    public float FarPlane  { get; set; }
}