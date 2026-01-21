#version 330 core

layout (location = 0) in int aBlock;

out int vBlock;
out vec3 vWorldPos;

uniform vec3 uChunkPos;
uniform int size;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    vBlock = aBlock;
    vec3 pos = uChunkPos + vec3(mod(gl_VertexID, size), mod(gl_VertexID / size, size), gl_VertexID / (size * size));
    vWorldPos = pos;
    gl_Position = projection * view * vec4(pos, 1.0);
}