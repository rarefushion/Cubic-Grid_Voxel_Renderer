using System.Runtime.InteropServices;

namespace GalensUnified.CubicGrid.Renderer.NET;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Model
{
    public fixed int textureIDs[6];
    public fixed int shapeIDs[6];
    public fixed int anyFaceVisible[6];

    public Model
    (
        int faceBack, int faceFront, int faceTop, int faceBottom, int faceLeft, int faceRight,
        int shapeBack, int shapeFront, int shapeTop, int shapeBottom, int shapeLeft, int shapeRight,
        bool anyVisibleBackFlag, bool anyVisibleFrontFlag, bool anyVisibleTopFlag, bool anyVisibleBottomFlag, bool anyVisibleLeftFlag, bool anyVisibleRightFlag
    )
    {
        textureIDs[0] = faceBack;
        textureIDs[1] = faceFront;
        textureIDs[2] = faceTop;
        textureIDs[3] = faceBottom;
        textureIDs[4] = faceLeft;
        textureIDs[5] = faceRight;
        shapeIDs[0] = shapeBack;
        shapeIDs[1] = shapeFront;
        shapeIDs[2] = shapeTop;
        shapeIDs[3] = shapeBottom;
        shapeIDs[4] = shapeLeft;
        shapeIDs[5] = shapeRight;
        anyFaceVisible[0] = anyVisibleBackFlag ? 1 : 0;
        anyFaceVisible[1] = anyVisibleFrontFlag ? 1 : 0;
        anyFaceVisible[2] = anyVisibleTopFlag ? 1 : 0;
        anyFaceVisible[3] = anyVisibleBottomFlag ? 1 : 0;
        anyFaceVisible[4] = anyVisibleLeftFlag ? 1 : 0;
        anyFaceVisible[5] = anyVisibleRightFlag ? 1 : 0;
    }
}