#version 430 core
#if defined(GL_NV_gpu_shader5)
    #extension GL_NV_gpu_shader5 : enable
#elif defined(GL_EXT_shader_explicit_arithmetic_types_int16)
    #extension GL_EXT_shader_explicit_arithmetic_types_int16 : enable
#endif

layout(local_size_x = 16, local_size_y = 16, local_size_z = 1) in;
// This compute writes to chunksOccluded, one int is a 'word' or 32 bits each representing a chunk. 1 render 0 don't render
layout(binding=2) buffer OccludedChunkBuffer { flat int chunksOccluded[]; };
layout(binding=0) buffer ChunkBuffer { flat uint16_t chunks[]; };

// Assign on program creation
uniform float nearClip;
uniform int chunkLength;
uniform int chunkVolume;
uniform int worldLength;
uniform int worldChunkLength;
// Assign every execution
uniform vec2 screenSize;
uniform mat4 projectionInverse;
uniform mat4 viewInverse;
uniform int maxSteps;
uniform vec3 negBounds;
uniform vec3 posBounds;

void main()
{
    // Get raycast dir
    // 1. Calculate screen pos in [-1, 1] range
    vec2 uv = (vec2(gl_GlobalInvocationID.xy) + 0.5) / vec2(screenSize) * 2.0 - 1.0;
    // 2. Unproject to View Space
    vec4 target = projectionInverse * vec4(uv, -1.0, 1.0);
    vec3 rayDirView = normalize(target.xyz / target.w);
    // 3. Transform direction to World Space
    vec3 rayDir = mat3(viewInverse) * rayDirView;
    // Get raycast pos
    vec3 rayPos = viewInverse[3].xyz + rayDir * nearClip;
    // Account for ray outside world
    vec3 invRayDir = 1.0 / rayDir;
    vec3 negBoundsDis = (negBounds - rayPos) * invRayDir;
    vec3 posBoundsDis = (posBounds - rayPos) * invRayDir;
    vec3 closestBoundsWall = min(negBoundsDis, posBoundsDis);
    vec3 farthestBoundsWall = max(negBoundsDis, posBoundsDis);
    float enterDis = max(max(closestBoundsWall.x, closestBoundsWall.y), closestBoundsWall.z);
    float exitDis = min(min(farthestBoundsWall.x,  farthestBoundsWall.y),  farthestBoundsWall.z);
    float passThroughWorld = step(enterDis, exitDis) * step(0.0, enterDis) * step(0.0, exitDis);
    rayPos += rayDir * enterDis * passThroughWorld * 1.001;
    // Raycast check every block and flag chunk
    vec3 sideStep = sign(rayDir);
    vec3 deltaDist = abs(invRayDir);
    vec3 blockPos = floor(rayPos);
    vec3 sideDist = (sideStep * (blockPos - rayPos) + (sideStep * 0.5) + 0.5) * deltaDist;
    float invChunkLength = 1.0 / chunkLength;
    float invWorldLength = 1.0 / worldLength;
    for (int i = 0; i < maxSteps; i++)
    {
        if
        (
            blockPos.x >= posBounds.x || blockPos.x < negBounds.x ||
            blockPos.y >= posBounds.y || blockPos.y < negBounds.y ||
            blockPos.z >= posBounds.z || blockPos.z < negBounds.z
        )
            break;
        vec3 rayPosWorldWrap = blockPos - worldLength * floor(blockPos * invWorldLength);
        vec3 chunkCoord = floor(rayPosWorldWrap * invChunkLength);
        uint chunkOccludedIndex = uint((chunkCoord.z * worldChunkLength + chunkCoord.y) * worldChunkLength + chunkCoord.x);
        uint chunkIndex = chunkOccludedIndex * chunkVolume;
        vec3 blockLocalPos = rayPosWorldWrap - chunkCoord * chunkLength;
        uint localBlockIndex = uint((blockLocalPos.z * chunkLength + blockLocalPos.y) * chunkLength + blockLocalPos.x);
        uint blockIndex = chunkIndex + localBlockIndex;
        if (chunks[blockIndex] > 0us)
        {
            // 32 size of uint
            // 1u is uint 1 in decimal (0x31)1
            // << bitwise left-shift moves the left most bit(1) by so many spaces
            // atomicOr is thread safe bitwise OR
            atomicOr(chunksOccluded[chunkOccludedIndex / 32], (1 << (chunkOccludedIndex % 32)));
            break;
        }
        vec3 mask = step(sideDist.xyz, sideDist.yzx) * step(sideDist.xyz, sideDist.zxy);
        sideDist += mask * deltaDist;
        blockPos += mask * sideStep;
    }
}