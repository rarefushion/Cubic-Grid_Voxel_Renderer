#include "GridAlignedQuadCommon.hlsli"
// WorldPositionToClip must be defined after including GridAlignedQuadCommon.hlsli when using CreateShapeVaryings.

cbuffer Uniforms : register(b0)
{
    float4x4 view;
    float4x4 projection;
    float3 chunkPos;
};

StructuredBuffer<QuadInstance> QuadInstances : register(t0);

float4 WorldPositionToClip(float3 worldPosition) { return mul(mul(float4(worldPosition, 1.0), view), projection); }

ShapeVaryings VSMain(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
{
    QuadInstance instance = QuadInstances[instanceID];
    return CreateShapeVaryings(instance, instanceID, vertexID, chunkPos);
}