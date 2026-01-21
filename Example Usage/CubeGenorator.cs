using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.GLFW;
using StbImageSharp;

public unsafe static class CubeGenorator
{
    public static MeshMaterial material;

    public readonly static int[] vertIndexForTris = [0, 1, 2, 2, 1, 3];
    public readonly static int[] quads = [0, 3, 1, 2, 5, 6, 4, 7, 3, 7, 2, 6, 1, 5, 0, 4, 4, 7, 0, 3, 1, 2, 5, 6]; //This is a 2D array just need to index 4 * face
    public readonly static Vector3[] blockVertices =
    [
        Vector3.Zero, new(1.0f, 0.0f, 0.0f), new(1.0f, 1.0f, 0.0f), new(0.0f, 1.0f, 0.0f),
        new(0.0f, 0.0f, 1.0f), new(1.0f, 0.0f, 1.0f), new(1.0f, 1.0f, 1.0f), new(0.0f, 1.0f, 1.0f)
    ];

    public readonly static Vector3[] directions =
        [ new(0f, 0f, -1), new(0f, 0f, 1), new(0f, 1f, 0f), new(0f, -1f, 0f), new(-1f, 0f, 0f), new(1f, 0f, 0f) ];
    public readonly static Vector2[] uvOffsets =
        [ Vector2.Zero, Vector2.UnitY, Vector2.UnitX, Vector2.One ];

    public static void Load()
    {
        material = new MeshMaterial(Program.mainCam);
        static bool IsNegitive(Vector3 dir) => dir.X < 0f || dir.Y < 0f || dir.Z < 0f;
        foreach (Vector3 dir in BlockInfo.directions)
            SpawnCube(dir * 10f, dir * (IsNegitive(dir) ? -0.25f : 1f));
    }

    
    public static void SpawnCube(Vector3 pos, Vector3 color)
    {
        MeshMaterial.Vertex[] verts = new MeshMaterial.Vertex[6 * 4];
        uint[] indices = new uint[6 * 6];
        for (int face = 0; face < 6; face++)
        {
            for (int vert = 0; vert < 4; vert++)
                verts[vert + (face * 4)] = 
                    new MeshMaterial.Vertex() 
                    { 
                        Position = blockVertices[quads[4 * face + vert]] + pos, 
                        Color = color, 
                        UV = uvOffsets[vert]
                    };

            for (int indice = 0; indice < 6; indice++)
                indices[indice + (face * 6)] = (uint)(vertIndexForTris[indice] + (face * 4));
        }

        // Load Atlas
        Image img;
        using (Stream stream = File.OpenRead(Path.Combine(Program.assets.FullName, "White.png")))
        {
            ImageResult imageResult = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            img = new Image
                    {
                        Width = imageResult.Width,
                        Height = imageResult.Height,
                        // Pin the byte array in memory and get a pointer to the raw pixel data
                        Pixels = (byte*)Marshal.AllocHGlobal(imageResult.Data.Length)
                    };

            // Copy the data to the unmanaged memory
            Marshal.Copy(imageResult.Data, 0, (IntPtr)img.Pixels, imageResult.Data.Length);
        }

        material.CreateMesh(verts, indices, GraphicsLibrary.FlipImage(img));
    }
}