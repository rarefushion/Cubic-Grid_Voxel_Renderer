#include "Common.hlsli"

cbuffer Uniforms : register(b0)
{
    float4x4 view;
    float4x4 projection;
    float3 chunkPos;
    int verticesPerShape;
};

StructuredBuffer<ShapeVertex> ShapeVertices : register(t3);

StructuredBuffer<ShapeInstance> ShapeInstances : register(t0);

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
