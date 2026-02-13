#version 430 core

layout(binding=0) buffer ChunkBuffer { flat int chunks[]; };

out float vBlock;
out vec3 vWorldPos;

uniform int uChunkIndex;
uniform vec3 uChunkPos;
uniform float size;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    int volume = int(size + 0.5) * int(size + 0.5) * int(size + 0.5);
    float index = float(gl_VertexID);
    vec3 pos = uChunkPos + vec3(floor(mod(index, size)), floor(mod(index / size, size)), floor(index / (size * size)));
    vWorldPos = pos;
    gl_Position = projection * view * vec4(pos, 1.0);
    vBlock = chunks[volume * uChunkIndex + gl_VertexID];
}
