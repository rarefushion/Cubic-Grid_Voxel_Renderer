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

out flat int gBlock;
out vec2 gUV;
out flat int gFace;

uniform vec3 chunkPos;

uniform mat4 projection;
uniform mat4 view;

void main()
{
    BlockVertex vert = blockVertices[gl_VertexID];

    gBlock = aBlock;
    gUV = vert.uv;
    gFace = vert.face;

    gl_Position = projection * view * vec4(vert.position + chunkPos + aPos, 1.0);
}
