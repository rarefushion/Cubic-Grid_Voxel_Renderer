#version 430 core
layout(local_size_x = 64) in;

struct FaceInstance
{
    float posX; float posY; float posZ;
    int block;
    float brightness;
    int face;
};

layout(std430, binding=0) buffer FaceInstances { FaceInstance[] blockVertices; };

uniform uint bufferStart;
uniform uint bufferEnd;
uniform vec3 chunkPos;
uniform bool setBase;
uniform float lightBase;
uniform float lightHit;
uniform float lightMiss;
uniform vec3 lightDirection;
uniform bool diffuseShading;
uniform float lightMin;
uniform float lightMax;

const vec3[] directions = vec3[]
(
    vec3( 0.0, 0.0,-1.0),
    vec3( 0.0, 0.0, 1.0),
    vec3( 0.0, 1.0, 0.0),
    vec3( 0.0,-1.0, 0.0),
    vec3(-1.0, 0.0, 0.0),
    vec3( 1.0, 0.0, 0.0)
);

const vec3[] centerOffsets = vec3[]
(
    vec3(0.5, 0.5, 0.0),
    vec3(0.5, 0.5, 1.0),
    vec3(0.5, 1.0, 0.5),
    vec3(0.5, 0.0, 0.5),
    vec3(0.0, 0.5, 0.5),
    vec3(1.0, 0.5, 0.5)
);

bool Raycast(vec3 position, vec3 direction);

void main()
{
    uint index = bufferStart + gl_GlobalInvocationID.x;
    FaceInstance instance = blockVertices[index];
    if (index >= bufferEnd)
        return;
    if (setBase)
        blockVertices[index].brightness = lightBase;
    float lightDot = dot(lightDirection, directions[instance.face]);
    vec3 blockPos = chunkPos + vec3(instance.posX, instance.posY, instance.posZ) + centerOffsets[instance.face];
    if (lightDot > 0.0)
    {
        if (Raycast(blockPos, lightDirection))
            blockVertices[index].brightness += lightMiss;
        else
            blockVertices[index].brightness += diffuseShading ? lightHit * lightDot : lightHit;
    }
    else
        blockVertices[index].brightness += lightMiss;
    blockVertices[index].brightness = clamp(blockVertices[index].brightness, lightMin, lightMax);
}