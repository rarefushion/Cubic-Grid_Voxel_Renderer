cbuffer Uniforms : register(b0)
{
    float4x4 view;
    float4x4 projection;
    float3 chunkPos;
    int verticesPerShape;
};

struct ShapeVertex
{
    float3 position;
    float _pad0;
    float2 uv;
    float _pad2;
    float _pad1;
};

StructuredBuffer<ShapeVertex> ShapeVertices : register(t3);

struct ShapeInstance
{
    float posX, posY, posZ;
    int textureIndex;
    uint16_t shape;
    uint16_t rotation;
    float tintR, tintG, tintB;
};

StructuredBuffer<ShapeInstance> ShapeInstances : register(t0);

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

struct ShapeVaryings
{
    float4 position : SV_POSITION;
    float2 UV : TEXCOORD0;
    nointerpolation int textureIndex : TEXCOORD1;
    float3 tint : TEXCOORD2;
};

ShapeVaryings VSMain(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
{
    ShapeVaryings output;
    ShapeInstance instance = ShapeInstances[instanceID];
    ShapeVertex vert = ShapeVertices[vertexID + int(instance.shape) * verticesPerShape];

    output.UV = vert.uv;
    output.textureIndex = instance.textureIndex;
    output.tint = float3(instance.tintR, instance.tintG, instance.tintB);

    float3 rotatedVert = mul(vert.position - 0.5, ROTATION[int(instance.rotation)]) + 0.5;
    float3 instancePos = float3(instance.posX, instance.posY, instance.posZ);
    float3 worldPos = rotatedVert + chunkPos + instancePos;
    output.position = mul(mul(float4(worldPos, 1.0), view), projection);

    return output;
}
