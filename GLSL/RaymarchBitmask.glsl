#version 430 core

layout(binding=4) buffer RaymarchBitmask { uint blockMask[]; };

uniform int chunkLength;
uniform int maxRaydistance;
uniform ivec3 worldOrigin;
uniform ivec3 worldDimensionsInChunks;

bool Raycast(vec3 position, vec3 direction)
{
    ivec3 worldMax = worldOrigin + worldDimensionsInChunks * chunkLength;
    int uintsPerChunk = (chunkLength * chunkLength * chunkLength) / 32;
    ivec3 blockPos = ivec3(floor(position));
    int stepX = direction.x < 0 ? -1 : 1;
    int stepY = direction.y < 0 ? -1 : 1;
    int stepZ = direction.z < 0 ? -1 : 1;
    int maxWorldX = direction.x < 0 ? int(worldOrigin.x) : worldMax.x;
    int maxWorldY = direction.y < 0 ? int(worldOrigin.y) : worldMax.y;
    int maxWorldZ = direction.z < 0 ? int(worldOrigin.z) : worldMax.z;
    float deltaDistX = abs(1.0 / direction.x);
    float deltaDistY = abs(1.0 / direction.y);
    float deltaDistZ = abs(1.0 / direction.z);
    float sideDistX = direction.x < 0 ? (position.x - float(blockPos.x)) * deltaDistX : (float(blockPos.x) + 1.0 - position.x) * deltaDistX;
    float sideDistY = direction.y < 0 ? (position.y - float(blockPos.y)) * deltaDistY : (float(blockPos.y) + 1.0 - position.y) * deltaDistY;
    float sideDistZ = direction.z < 0 ? (position.z - float(blockPos.z)) * deltaDistZ : (float(blockPos.z) + 1.0 - position.z) * deltaDistZ;
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

        if (blockPos.x == maxWorldX || blockPos.y == maxWorldY || blockPos.z == maxWorldZ)
            break;

        ivec3 wrappedPos = blockPos - worldOrigin;
        ivec3 chunkPos = wrappedPos / chunkLength;
        ivec3 localPos = wrappedPos % chunkLength;
        int chunkIndex = (chunkPos.z * worldDimensionsInChunks.y + chunkPos.y) * worldDimensionsInChunks.x + chunkPos.x;
        int localBlockIndex = (localPos.z * chunkLength + localPos.y) * chunkLength + localPos.x;
        // Target the specific uint in the global array
        int globalUintIndex = (chunkIndex * uintsPerChunk) + (localBlockIndex / 32);
        int bitIndex = localBlockIndex % 32;
        uint bitMask = 1u << bitIndex;
        if ((blockMask[globalUintIndex] & bitMask) != 0u)
            return true;
    }
    return false;
}