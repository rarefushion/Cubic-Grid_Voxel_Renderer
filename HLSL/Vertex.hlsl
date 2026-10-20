#include "Common.hlsli"
// WorldPositionToClip must be defined after including Common.hlsli when using CreateShapeVaryings.

cbuffer Uniforms : register(b0)
{
    float4x4 view;
    float4x4 projection;
    float3 chunkPos;
    int verticesPerShape;
};

StructuredBuffer<ShapeVertex> ShapeVertices : register(t3);

StructuredBuffer<ShapeInstance> ShapeInstances : register(t0);

float4 WorldPositionToClip(float3 worldPosition) { return mul(mul(float4(worldPosition, 1.0), view), projection); }

ShapeVaryings VSMain(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
{
    ShapeInstance instance = ShapeInstances[instanceID];
    ShapeVertex vert = ShapeVertices[vertexID + ShapeIndex(instance) * verticesPerShape];
    return CreateShapeVaryings(instance, vert, chunkPos);
}
