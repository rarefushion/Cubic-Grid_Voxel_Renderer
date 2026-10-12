#version 450

uniform sampler2DArray SPIRV_Cross_CombinedtextureArraytextureSampler;

layout(location = 0) in vec2 in_var_TEXCOORD0;
layout(location = 1) flat in int in_var_TEXCOORD1;
layout(location = 2) in vec3 in_var_TEXCOORD2;
layout(location = 0) out vec4 out_var_SV_TARGET;

void main()
{
    vec4 _37 = texture(SPIRV_Cross_CombinedtextureArraytextureSampler, vec3(in_var_TEXCOORD0, float(in_var_TEXCOORD1)));
    float _38 = _37.w;
    if (_38 < 0.00999999977648258209228515625)
    {
        discard;
    }
    out_var_SV_TARGET = vec4(_37.xyz * in_var_TEXCOORD2, _38);
}

