#version 430 core

layout(binding=4) buffer RaymarchBitmask { uint[] blockMask; };

uniform int maxRaydistance;
uniform ivec3 worldOrigin;
uniform ivec3 worldDimensions;

bool Raycast(vec3 position, vec3 direction)
{
    ivec3 blockPos = ivec3(floor(position));
    int stepX = direction.x < 0 ? -1 : 1;
    int stepY = direction.y < 0 ? -1 : 1;
    int stepZ = direction.z < 0 ? -1 : 1;
    float deltaDistX = abs(1f / direction.x);
    float deltaDistY = abs(1f / direction.y);
    float deltaDistZ = abs(1f / direction.z);
    float sideDistX = direction.x < 0 ? (position.x - float(blockPos.x)) * deltaDistX : (float(blockPos.x) + 1f - position.x) * deltaDistX;
    float sideDistY = direction.y < 0 ? (position.y - float(blockPos.y)) * deltaDistY : (float(blockPos.y) + 1f - position.y) * deltaDistY;
    float sideDistZ = direction.z < 0 ? (position.z - float(blockPos.z)) * deltaDistZ : (float(blockPos.z) + 1f - position.z) * deltaDistZ;
    blockPos = blockPos - worldOrigin;
    for (int i = 0; i < maxRaydistance; i++)
    {
        // Step along the shortest sideDist
        if (sideDistX < sideDistY && sideDistX < sideDistZ)
        {
            sideDistX += deltaDistX;
            blockPos.x += stepX;
        }
        else if (sideDistY < sideDistZ)
        {
            sideDistY += deltaDistY;
            blockPos.y += stepY;
        }
        else
        {
            sideDistZ += deltaDistZ;
            blockPos.z += stepZ;
        }

        if
        (
            blockPos.x < 0 || blockPos.x >= worldDimensions.x ||
            blockPos.y < 0 || blockPos.y >= worldDimensions.y ||
            blockPos.z < 0 || blockPos.z >= worldDimensions.z
        )
            break;
        int index = blockPos.x + blockPos.y * worldDimensions.x + blockPos.z * worldDimensions.x * worldDimensions.y;
        int uintIndex = index / 32;
        int bitIndex = index % 32;
        if ((blockMask[uintIndex] & (1u << bitIndex)) != 0u)
            return true;
    }
    return false;
}