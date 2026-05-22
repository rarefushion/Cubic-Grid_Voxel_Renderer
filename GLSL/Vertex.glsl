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
layout(location=1) in int aBlock;
layout(location=2) in float aBrightness;
layout(location=3) in int aFace;

out flat int vBlock;
out vec2 vUV;
out flat int vFace;
out float vBrightness;

uniform vec3 chunkPos;

uniform mat4 projection;
uniform mat4 view;

void main()
{
    BlockVertex vert = blockVertices[gl_VertexID + aFace * 6];

    vBlock = aBlock;
    vUV = vert.uv;
    vFace = aFace;
    vBrightness = aBrightness;

    gl_Position = projection * view * vec4(vert.position + chunkPos + aPos, 1.0);
}
