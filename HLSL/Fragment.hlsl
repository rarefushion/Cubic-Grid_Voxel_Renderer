#include "Common.hlsli"

Texture2DArray textureArray : register(t0);
SamplerState textureSampler : register(s0);

float4 PSMain(ShapeVaryings input) : SV_TARGET
{
    float4 textureColor = textureArray.Sample(textureSampler, float3(input.UV, (float)input.textureIndex));
    if (textureColor.a < 0.01)
        discard;
    return float4(textureColor.rgb * input.tint, textureColor.a);
}
