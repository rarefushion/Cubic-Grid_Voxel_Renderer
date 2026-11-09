#include "Common.hlsli"
// Texture2DArraySample must be defined after including Common.hlsli when using ShapeShade.

Texture2DArray textureArray : register(t0);
SamplerState textureSampler : register(s0);
ByteAddressBuffer tints : register(t1);

float4 Texture2DArraySample(float3 textureSample) { return textureArray.Sample(textureSampler, textureSample); }
float3 TintSample(int tintIndex) { return asfloat(tints.Load3(tintIndex * 12)); }

float4 PSMain(ShapeVaryings input) : SV_TARGET
{
    return ShapeShade(input);
}
