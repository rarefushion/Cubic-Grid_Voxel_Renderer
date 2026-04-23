#version 430 core
#if defined(GL_NV_gpu_shader5)
    #extension GL_NV_gpu_shader5 : enable
#elif defined(GL_EXT_shader_explicit_arithmetic_types_int16)
    #extension GL_EXT_shader_explicit_arithmetic_types_int16 : enable
#endif

struct BlockVertex
{
    vec3 position;
    float _pad0;
    vec2 uv;
    int face;
    float _pad1;
};

layout(binding=3) buffer BlockVertices { BlockVertex[] blockVertices; };
layout(binding=0) buffer ChunkBuffer { flat uint16_t chunks[]; };

out flat int gBlock;
out vec2 gUV;
out flat int gFace;

uniform int chunkIndex;
uniform vec3 chunkPos;
uniform int chunkLength;

uniform mat4 projection;
uniform mat4 view;

void main()
{
    uint16_t block = chunks[chunkIndex + gl_InstanceID];
    BlockVertex vert = blockVertices[gl_VertexID];

    gBlock = int(block);
    gUV = vert.uv;
    gFace = vert.face;

    if (block == 0us)
        gl_Position = vec4(0);
    else
    {
        vec3 blockPos =
            chunkPos +
            vec3
            (
                mod(gl_InstanceID, chunkLength),
                mod(gl_InstanceID / chunkLength, chunkLength),
                (gl_InstanceID / (chunkLength * chunkLength))
            );
        gl_Position = projection * view * vec4(vert.position + blockPos, 1.0);
    }
}
