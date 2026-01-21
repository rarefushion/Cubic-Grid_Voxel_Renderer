using System.Numerics;
using Silk.NET.Input;

public class DebugCamera : ICamera
{
    public Vector3 Position { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float Roll { get; set; }
    public Quaternion QuaternionRotation => Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, Roll);
    public Vector3 Facing => Vector3.Transform(-Vector3.UnitZ, QuaternionRotation);
    public Vector3 Up => Vector3.Transform(Vector3.UnitY, QuaternionRotation);
    public float Fov { get; set; } = MathF.PI * (120f / 360f);
    public float AspectRatio { get; set; } = Program.window.Size.X / (float)Program.window.Size.Y;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; }  = 2000f;

    static Vector3 toMove;
    static Vector2 lastMousePos;
    static bool firstMouse = true;

    static float mouseSensitivity = 0.0025f;
    static float moveSpeed = 2f;
    

    public void CameraRotate(IMouse mouse, Vector2 pos)
    {
        if (mouse.Cursor.CursorMode != CursorMode.Raw)
            return;
        if (firstMouse)
        {
            lastMousePos = pos;
            firstMouse = false;
            return;
        }

        Vector2 delta = pos - lastMousePos;
        lastMousePos = pos;

        Yaw   -= delta.X * mouseSensitivity;
        Pitch -= delta.Y * mouseSensitivity;

        // clamp pitch to avoid flipping
        float limit = MathF.PI / 2f - 0.01f;
        Pitch = Math.Clamp(Pitch, -limit, limit);
    }

    public void CameraMove(double deltaTime)
    {
        IKeyboard keyboard = Program.input.Keyboards[0];
        Vector3 dir = new(-MathF.Sin(Yaw), 0, -(float)Math.Cos(Yaw));
        if (keyboard.IsKeyPressed(Key.A))
            toMove = new Vector3(-dir.Z, 0, dir.X) * -1;
        else if (keyboard.IsKeyPressed(Key.D))
            toMove = new Vector3(-dir.Z, 0, dir.X) * 1;

        if (keyboard.IsKeyPressed(Key.S))
            toMove += dir * -1;
        else if (keyboard.IsKeyPressed(Key.W))
            toMove += dir * 1;

        if (keyboard.IsKeyPressed(Key.Space))
            toMove.Y = 1;
        else if (keyboard.IsKeyPressed(Key.ShiftLeft))
            toMove.Y = -1;

        float speedMult = Program.input.Mice[0].ScrollWheels[0].Y;
        speedMult = (speedMult > 0) ? 1.25f : (speedMult < 0) ? 0.75f : 0;
        if (speedMult != 0)
            moveSpeed *= speedMult;

        
        Position += toMove * (float)deltaTime * moveSpeed;
        toMove = new();
    }

    public DebugCamera()
    {
        Program.window.Render += CameraMove;

        Program.input.Mice[0].MouseMove += CameraRotate;
        Program.input.Mice[0].Cursor.CursorMode = CursorMode.Raw;
    }
}