#version 450
#ifdef GL_ARB_shader_draw_parameters
#extension GL_ARB_shader_draw_parameters : enable
#endif

struct QuadInstance
{
    uint posXYZAndNormal;
    int textureIndex;
};

const int _87[6] = int[](0, 1, 2, 2, 1, 3);
const int _88[24] = int[](0, 3, 1, 2, 5, 6, 4, 7, 3, 7, 2, 6, 1, 5, 0, 4, 4, 7, 0, 3, 1, 2, 5, 6);
const vec3 _89[8] = vec3[](vec3(0.0), vec3(1.0, 0.0, 0.0), vec3(1.0, 1.0, 0.0), vec3(0.0, 1.0, 0.0), vec3(0.0, 0.0, 1.0), vec3(1.0, 0.0, 1.0), vec3(1.0), vec3(0.0, 1.0, 1.0));
const vec2 _90[4] = vec2[](vec2(0.0), vec2(0.0, 1.0), vec2(1.0, 0.0), vec2(1.0));

struct type_Uniforms
{
    mat4 view;
    mat4 projection;
    vec3 chunkPos;
};

uniform type_Uniforms Uniforms;

layout(binding = 0, std430) readonly buffer type_StructuredBuffer_QuadInstance
{
    QuadInstance _m0[];
} QuadInstances;

layout(binding = 1, std430) readonly buffer type_ByteAddressBuffer
{
    uint _m0[];
} Tints;

#ifdef GL_ARB_shader_draw_parameters
#define SPIRV_Cross_BaseInstance gl_BaseInstanceARB
#else
uniform int SPIRV_Cross_BaseInstance;
#endif
layout(location = 0) out vec2 out_var_TEXCOORD0;
layout(location = 1) flat out int out_var_TEXCOORD1;
layout(location = 2) out vec3 out_var_TEXCOORD2;

mat4 spvWorkaroundRowMajor(mat4 wrap) { return wrap; }

void main()
{
    uint _101 = (uint((gl_InstanceID + SPIRV_Cross_BaseInstance)) * 12u) >> 2u;
    uint _119 = QuadInstances._m0[uint((gl_InstanceID + SPIRV_Cross_BaseInstance))].posXYZAndNormal >> 24u;
    gl_Position = spvWorkaroundRowMajor(Uniforms.projection) * (spvWorkaroundRowMajor(Uniforms.view) * vec4((Uniforms.chunkPos + vec3(uvec4(QuadInstances._m0[uint((gl_InstanceID + SPIRV_Cross_BaseInstance))].posXYZAndNormal & 255u, (QuadInstances._m0[uint((gl_InstanceID + SPIRV_Cross_BaseInstance))].posXYZAndNormal >> 8u) & 255u, (QuadInstances._m0[uint((gl_InstanceID + SPIRV_Cross_BaseInstance))].posXYZAndNormal >> 16u) & 255u, _119).xyz)) + _89[_88[(4u * _119) + uint(_87[uint(gl_VertexID)])]], 1.0));
    out_var_TEXCOORD0 = _90[_87[uint(gl_VertexID)]];
    out_var_TEXCOORD1 = QuadInstances._m0[uint((gl_InstanceID + SPIRV_Cross_BaseInstance))].textureIndex;
    out_var_TEXCOORD2 = uintBitsToFloat(uvec3(Tints._m0[_101], Tints._m0[_101 + 1u], Tints._m0[_101 + 2u]));
}

