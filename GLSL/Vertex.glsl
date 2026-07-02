#version 430 core

struct BlockVertex
{
    vec3 position;
    float _pad0;
    vec2 uv;
    int face;
    float _pad1;
};

layout(binding=3) buffer BlockVertices { BlockVertex[] blockVertices; };

layout(location=0) in vec3 aPos;
layout(location=1) in int aTexture;
layout(location=2) in int aFace;
layout(location=3) in vec3 aTint;

out vec2 vUV;
out flat int vTexture;
out vec3 vTint;

uniform vec3 chunkPos;

uniform mat4 projection;
uniform mat4 view;

void main()
{
    BlockVertex vert = blockVertices[gl_VertexID + aFace * 6];

    vUV = vert.uv;
    vTexture = aTexture;
    vTint = aTint;

    gl_Position = projection * view * vec4(vert.position + chunkPos + aPos, 1.0);
}
