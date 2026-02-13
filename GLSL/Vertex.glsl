#version 430 core

layout(binding=0) buffer ChunkBuffer { flat int chunks[]; };

out flat int vBlock;
out vec3 vWorldPos;

uniform int uChunkIndex;
uniform vec3 uChunkPos;
uniform int length;
uniform int volume;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    vBlock = chunks[volume * uChunkIndex + gl_VertexID];
    vWorldPos = uChunkPos + vec3(mod(gl_VertexID, length), mod(gl_VertexID / length, length), gl_VertexID / (length * length));
}
