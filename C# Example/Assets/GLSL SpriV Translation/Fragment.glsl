#version 450

layout(binding = 1, std430) readonly buffer type_ByteAddressBuffer
{
    uint _m0[];
} tints;

uniform sampler2DArray SPIRV_Cross_CombinedtextureArraytextureSampler;

layout(location = 0) in vec2 in_var_TEXCOORD0;
layout(location = 1) flat in int in_var_TEXCOORD1;
layout(location = 2) flat in int in_var_TEXCOORD2;
layout(location = 0) out vec4 out_var_SV_TARGET;

void main()
{
    vec4 _47 = texture(SPIRV_Cross_CombinedtextureArraytextureSampler, vec3(in_var_TEXCOORD0, float(in_var_TEXCOORD1)));
    float _48 = _47.w;
    if (_48 < 0.00999999977648258209228515625)
    {
        discard;
    }
    uint _54 = uint(in_var_TEXCOORD2 * 12) >> 2u;
    out_var_SV_TARGET = vec4(_47.xyz * uintBitsToFloat(uvec3(tints._m0[_54], tints._m0[_54 + 1u], tints._m0[_54 + 2u])), _48);
}

