#version 330 core

// This needs to be a float. shader code hates whole numbers.
layout (location = 0) in float aBlock;

out float vBlock;
out vec3 vWorldPos;

uniform vec3 uChunkPos;
uniform float size;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    vBlock = aBlock;
    float index = float(gl_VertexID);
    vec3 pos = uChunkPos + vec3(floor(mod(index, size)), floor(mod(index / size, size)), floor(index / (size * size)));
    vWorldPos = pos;
    gl_Position = projection * view * vec4(pos, 1.0);
}