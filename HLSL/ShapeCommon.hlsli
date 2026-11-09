#include "Common.hlsli"
// WorldPositionToClip must be defined after including Common.hlsli when using CreateShapeVaryings.

static const float3x3 ROTATION[24] =
{
    // up=-Z
    float3x3( 1,  0,  0,   0,  0, -1,   0,  1,  0),  //  0: fwd=+Y
    float3x3( 0,  1,  0,   0,  0, -1,  -1,  0,  0),  //  1: fwd=-X
    float3x3(-1,  0,  0,   0,  0, -1,   0, -1,  0),  //  2: fwd=-Y
    float3x3( 0, -1,  0,   0,  0, -1,   1,  0,  0),  //  3: fwd=+X
    // up=+Z
    float3x3( 1,  0,  0,   0,  0,  1,   0, -1,  0),  //  4: fwd=-Y
    float3x3( 0, -1,  0,   0,  0,  1,  -1,  0,  0),  //  5: fwd=-X
    float3x3(-1,  0,  0,   0,  0,  1,   0,  1,  0),  //  6: fwd=+Y
    float3x3( 0,  1,  0,   0,  0,  1,   1,  0,  0),  //  7: fwd=+X
    // up=+Y
    float3x3( 1,  0,  0,   0,  1,  0,   0,  0,  1),  //  8: fwd=+Z
    float3x3( 0,  0,  1,   0,  1,  0,  -1,  0,  0),  //  9: fwd=-X
    float3x3(-1,  0,  0,   0,  1,  0,   0,  0, -1),  // 10: fwd=-Z
    float3x3( 0,  0, -1,   0,  1,  0,   1,  0,  0),  // 11: fwd=+X
    // up=-Y
    float3x3(-1,  0,  0,   0, -1,  0,   0,  0,  1),  // 12: fwd=+Z
    float3x3( 0,  0, -1,   0, -1,  0,  -1,  0,  0),  // 13: fwd=-X
    float3x3( 1,  0,  0,   0, -1,  0,   0,  0, -1),  // 14: fwd=-Z
    float3x3( 0,  0,  1,   0, -1,  0,   1,  0,  0),  // 15: fwd=+X
    // up=-X
    float3x3( 0,  0,  1,  -1,  0,  0,   0, -1,  0),  // 16: fwd=-Y
    float3x3( 0,  1,  0,  -1,  0,  0,   0,  0,  1),  // 17: fwd=+Z
    float3x3( 0,  0, -1,  -1,  0,  0,   0,  1,  0),  // 18: fwd=+Y
    float3x3( 0, -1,  0,  -1,  0,  0,   0,  0, -1),  // 19: fwd=-Z
    // up=+X
    float3x3( 0,  0,  1,   1,  0,  0,   0,  1,  0),  // 20: fwd=+Y
    float3x3( 0,  1,  0,   1,  0,  0,   0,  0, -1),  // 21: fwd=-Z
    float3x3( 0,  0, -1,   1,  0,  0,   0, -1,  0),  // 22: fwd=-Y
    float3x3( 0, -1,  0,   1,  0,  0,   0,  0,  1)   // 23: fwd=+Z
};

struct ShapeVertex
{
    float3 position;
    float _pad0;
    float2 uv;
    float _pad2;
    float _pad1;
};

struct ShapeInstance
{
    float posX, posY, posZ;
    uint textureAndShapeAndRotation; // bytes 1 and 2 for texture index. byte 3 for shape index. byte 4 for rotation.
};
uint ShapeTexture(ShapeInstance instance) { return instance.textureAndShapeAndRotation & 0xFFFF; }
uint ShapeIndex(ShapeInstance instance) { return (instance.textureAndShapeAndRotation >> 16) & 0xFF; }
uint ShapeRotation(ShapeInstance instance) { return (instance.textureAndShapeAndRotation >> 24) & 0xFF; }

// The WorldPositionToClip function prototype in Common.hlsli must be defined if using CreateShapeVaryings.
ShapeVaryings CreateShapeVaryings(ShapeInstance instance, uint instanceID, ShapeVertex vert, float3 chunkPosition)
{
    float3 rotatedVert = mul(vert.position - 0.5, ROTATION[ShapeRotation(instance)]) + 0.5;
    float3 instancePos = float3(instance.posX, instance.posY, instance.posZ);
    float3 worldPos = rotatedVert + chunkPosition + instancePos;

    ShapeVaryings output;
    output.UV = vert.uv;
    output.UVMax = float2(1, 1);
    output.textureIndex = ShapeTexture(instance);
    output.tintIndex = instanceID;
    output.position = WorldPositionToClip(worldPos);

    return output;
}