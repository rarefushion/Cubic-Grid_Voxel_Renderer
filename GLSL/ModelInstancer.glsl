#version 430 core
#if defined(GL_NV_gpu_shader5)
    #extension GL_NV_gpu_shader5 : enable
#elif defined(GL_EXT_shader_explicit_arithmetic_types_int16)
    #extension GL_EXT_shader_explicit_arithmetic_types_int16 : enable
#endif

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;

struct RenderData
{
    int[6] textureIDs;
    int[6] shapeIDs;
    bool[6] anyFaceVisible;
};

struct ShapeInstance
{
    float posX, posY, posZ;
    int texture;
    uint16_t shape;
    uint16_t rotation;
    float tintR, tintG, tintB;
};

layout(binding=4) buffer BlockRenderDatas { RenderData[] renderDatas; };
layout(binding=0) buffer ShapeInstances { ShapeInstance[] shapeInstances; };
layout(binding=1) buffer FlattenedChunks { uint16_t[] flattenedChunks; };
layout(binding=5) uniform atomic_uint shapeCount;

uniform int chunkLength;
uniform int chunkVolume;
uniform int chunkOffset;
uniform vec3 chunkPos;
uniform int instanceOffset;
uniform int clusterLength;
uniform int clusterHeight;
uniform int clusterChunkLength;
uniform int clusterChunkHeight;

const ivec3 directions[6] = ivec3[6]
(
    ivec3( 0,  0, -1), // Back
    ivec3( 0,  0,  1), // Front
    ivec3( 0,  1,  0), // Top
    ivec3( 0, -1,  0), // Bottom
    ivec3(-1,  0,  0), // Left
    ivec3( 1,  0,  0)  // Right
);


ivec3 ClusterLocalPos(ivec3 pos)
{
    return ivec3
    (
        mod((mod(pos.x, clusterLength) + clusterLength), clusterLength),
        mod((mod(pos.y, clusterHeight) + clusterHeight), clusterHeight),
        mod((mod(pos.z, clusterLength) + clusterLength), clusterLength)
    );
}

ivec3 ChunkCoord(ivec3 pos) { return pos / chunkLength; }

int IndexByChunkCoord(ivec3 coord) { return ((coord.z * clusterChunkHeight + coord.y) * clusterChunkLength + coord.x) * chunkVolume; }
int ChunkIndex(ivec3 pos)
{
    pos = ivec3(mod(vec3(pos), chunkLength));
    return (pos.z * chunkLength + pos.y) * chunkLength + pos.x;
}

void main()
{
    uint x = gl_GlobalInvocationID.x;
    uint y = gl_GlobalInvocationID.y;
    uint z = gl_GlobalInvocationID.z;

    if (x >= uint(chunkLength) || y >= uint(chunkLength) || z >= uint(chunkLength))
        return;

    int chunkIndex = IndexByChunkCoord(ChunkCoord(ClusterLocalPos(ivec3(chunkPos))));
    int index = (int(z) * chunkLength + int(y)) * chunkLength + int(x);
    uint16_t block = flattenedChunks[chunkIndex + index];
    if (block == 0us)
        return;
    RenderData blockData = renderDatas[int(block)];
    ivec3 blockPos = ivec3(chunkPos) + ivec3(x, y, z);

    uint16_t neighborBlocks[6] = uint16_t[6](0us);
    bool visibleFaces[6] = bool[6](false);
    bool anyVisible = false;
    for (int f = 0; f < 6; f++)
    {
        if (blockData.shapeIDs[f] == 0)
            continue;
        ivec3 testPos = ClusterLocalPos(blockPos + directions[f]);
        int testChunkIndex = IndexByChunkCoord(ChunkCoord(testPos));
        int testBlockIndex = ChunkIndex(testPos);
        neighborBlocks[f] = flattenedChunks[testChunkIndex + testBlockIndex];
        if (neighborBlocks[f] != 0us)
            continue;
        visibleFaces[f] = true;
        anyVisible = true;
    }
    for (int f = 0; f < 6; f++)
    {
        if (!visibleFaces[f] && !(blockData.anyFaceVisible[f] && anyVisible))
            continue;
        uint instancesIndex = atomicCounterIncrement(shapeCount) + instanceOffset;
        shapeInstances[instancesIndex] = ShapeInstance
        (
            float(x), float(y), float(z),
            blockData.textureIDs[f],
            uint16_t(blockData.shapeIDs[f]),
            2us * 4us + 0us, // up, forward
            1.0, 1.0, 1.0 
        );
    }
}