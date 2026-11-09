struct ShapeVaryings
{
    float4 position : SV_POSITION;
    float2 UV : TEXCOORD0;
    nointerpolation float2 UVMax : TEXCOORD3;
    nointerpolation int textureIndex : TEXCOORD1;
    nointerpolation int tintIndex : TEXCOORD2;
};

// This function prototype must be defined if using CreateShapeVaryings in ShapeCommon.hlsli or GridAlignedQuadCommon.hlsli.
float4 WorldPositionToClip(float3 worldPosition);

// This function prototype must be defined if using ShapeShade.
float4 Texture2DArraySample(float3 textureSample);

// This function prototype must be defined if using ShapeShade.
float3 TintSample(ShapeVaryings varyings);

float4 ShapeShade(ShapeVaryings input)
{
    float4 texel = Texture2DArraySample(float3(input.UV, float(input.textureIndex)));
    if (texel.a < 0.01)
        discard;
    float3 tint = TintSample(input);
    return float4(texel.rgb * tint, texel.a);
}