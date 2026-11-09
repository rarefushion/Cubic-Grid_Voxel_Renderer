#include "Common.hlsli"
// WorldPositionToClip must be defined after including Common.hlsli when using CreateShapeVaryings.

// Index offset for creating the two triangles that make a quad.
static const int quadsOffsetForTris[6] = {0, 1, 2, 2, 1, 3};
// A 2D flattened array or indexing vertices. Indexed at 4 * face + quadOffset.
static const int quads[24] = {0, 3, 1, 2, 5, 6, 4, 7, 3, 7, 2, 6, 1, 5, 0, 4, 4, 7, 0, 3, 1, 2, 5, 6};
// 8 vertices of a cube. Bottom left, counter clock wise, -z corners then +z corners.
static const float3 vertices[8] =
{
    float3(0.0, 0.0, 0.0), float3(1.0, 0.0, 0.0), float3(1.0, 1.0, 0.0), float3(0.0, 1.0, 0.0),
    float3(0.0, 0.0, 1.0), float3(1.0, 0.0, 1.0), float3(1.0, 1.0, 1.0), float3(0.0, 1.0, 1.0)
};
// Texture UVs indexed with quadOffset.
static const float2 uvOffsets[4] =
{
    float2(0.0, 0.0),
    float2(0.0, 1.0),
    float2(1.0, 0.0),
    float2(1.0, 1.0)
};
static const float3 scaleXAxisMask[6] = { float3(1, 0, 0), float3(1, 0, 0), float3(1, 0, 0), float3(1, 0, 0), float3(0, 0, 1), float3(0, 0, 1) };
static const float3 scaleYAxisMask[6] = { float3(0, 1, 0), float3(0, 1, 0), float3(0, 0, 1), float3(0, 0, 1), float3(0, 1, 0), float3(0, 1, 0) };
static const float3 normalAxisMask[6] = { float3(0, 0, 1), float3(0, 0, 1), float3(0, 1, 0), float3(0, 1, 0), float3(1, 0, 0), float3(1, 0, 0) };

struct QuadInstance
{
    uint posXYZAndNormal; // bytes 1, 2, 3 for pos X, Y, Z respectively. byte 4 for normal face.
    uint textureAndScale; // bytes 1 and 2 for texture index. bytes 3, 4 for X, Y scale respectively.
    uint tintIndex;
};
uint4 QuadPositionNormal(QuadInstance instance)
{
    uint4 toReturn;
    toReturn.x = instance.posXYZAndNormal & 0xFF;
    toReturn.y = (instance.posXYZAndNormal >> 8) & 0xFF;
    toReturn.z = (instance.posXYZAndNormal >> 16) & 0xFF;
    toReturn.w = (instance.posXYZAndNormal >> 24);
    return toReturn;
}
int QuadTextureIndex(QuadInstance instance) { return instance.textureAndScale & 0xFFFF; }
float2 QuadScale(QuadInstance instance)
{
    float2 toReturn;
    toReturn.x = (instance.textureAndScale >> 16) & 0xFF;
    toReturn.y = (instance.textureAndScale >> 24) & 0xFF;
    return toReturn;
}

// The WorldPositionToClip function prototype located in Common.hlsli must be defined if using CreateShapeVaryings.
ShapeVaryings CreateShapeVaryings(QuadInstance instance, uint instanceID, uint vertexID, float3 chunkPosition)
{
    float2 instanceScale = QuadScale(instance) + float2(1, 1);
    uint4 posNormal = QuadPositionNormal(instance);
    float3 vertScale = scaleXAxisMask[posNormal.w] * instanceScale.x + scaleYAxisMask[posNormal.w] * instanceScale.y + normalAxisMask[posNormal.w];
    float3 vert = vertices[quads[4 * posNormal.w + quadsOffsetForTris[vertexID]]] * vertScale;
    float3 worldPosition = chunkPosition + posNormal.xyz + vert;

    ShapeVaryings output;
    output.UV = uvOffsets[quadsOffsetForTris[vertexID]] * instanceScale;
    output.UVMax = instanceScale;
    output.textureIndex = QuadTextureIndex(instance);
    output.tintIndex = instance.tintIndex;
    output.position = WorldPositionToClip(worldPosition);

    return output;
}