#include "Common.hlsli"
// Texture2DArraySample must be defined after including Common.hlsli when using ShapeShade.

Texture2DArray textureArray : register(t0);
SamplerState textureSampler : register(s0);
ByteAddressBuffer tints : register(t1);

float4 Texture2DArraySample(float3 textureSample) { return textureArray.Sample(textureSampler, textureSample); }
float3 TintSample(ShapeVaryings varyings)
{
    int2 uv = clamp(int2(floor(varyings.UV)), int2(0, 0), varyings.UVMax - 1);
    int tintIndex = varyings.tintIndex + uv.y * varyings.UVMax.x + uv.x;
    return asfloat(tints.Load3(tintIndex * 12));
}

float4 PSMain(ShapeVaryings input) : SV_TARGET
{
    return ShapeShade(input);
}
