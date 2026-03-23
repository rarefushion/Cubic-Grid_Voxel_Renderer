#version 430 core
#if defined(GL_NV_gpu_shader5)
    #extension GL_NV_gpu_shader5 : enable
#elif defined(GL_EXT_shader_explicit_arithmetic_types_int16)
    #extension GL_EXT_shader_explicit_arithmetic_types_int16 : enable
#endif

layout(binding=0) buffer ChunkBuffer { flat uint16_t chunks[]; };
layout (points) in;
layout (triangle_strip, max_vertices = 24) out;

in flat int vBlockIndex[];

out flat int gBlock;
out vec2 gUV;
out flat int gFace;

uniform int chunkIndex;
uniform int chunkLength;
uniform int chunkVolume;
uniform vec3 chunkPos;
uniform int worldLength;
uniform int worldChunkLength;

uniform mat4 projection;
uniform mat4 view;

void main()
{
    // Constants
    // quads is a 2D array just need to index 4 * face
    int quads[24] = int[](0, 3, 1, 2, 5, 6, 4, 7, 3, 7, 2, 6, 1, 5, 0, 4, 4, 7, 0, 3, 1, 2, 5, 6);
    vec3 blockVertices[8] = vec3[]
    (
        vec3(0.0, 0.0, 0.0), vec3(1.0, 0.0, 0.0), vec3(1.0, 1.0, 0.0), vec3(0.0, 1.0, 0.0),
        vec3(0.0, 0.0, 1.0), vec3(1.0, 0.0, 1.0), vec3(1.0, 1.0, 1.0), vec3(0.0, 1.0, 1.0)
    );
    vec3 directions[6] = vec3[]
    (
        vec3( 0.0,  0.0, -1.0),
        vec3( 0.0,  0.0,  1.0),
        vec3( 0.0,  1.0,  0.0),
        vec3( 0.0, -1.0,  0.0),
        vec3(-1.0,  0.0,  0.0),
        vec3( 1.0,  0.0,  0.0)
    );
    vec2[4] uvOffsets = vec2[]
        ( vec2(0.0, 0.0), vec2(0.0, 1.0), vec2(1.0, 0.0), vec2(1.0, 1.0) );

    int blockIndex = chunkIndex + vBlockIndex[0];
    uint16_t block = chunks[blockIndex];
    if (block == 0us)
        return;
    vec3 blockPos =
        chunkPos +
        vec3
        (
            mod(vBlockIndex[0], chunkLength),
            mod(vBlockIndex[0] / chunkLength, chunkLength),
            (vBlockIndex[0] / (chunkLength * chunkLength))
        );
    blockPos = floor(blockPos);

    // Create Faces
    gBlock = int(block);
    for (int f = 0; f < 6; f++)
    {
        vec3 checkingBlockPos = mod(mod(blockPos + directions[f], worldLength) + worldLength, worldLength);
        vec3 checkingChunkCoord = floor(checkingBlockPos / chunkLength);
        int checkingChunkIndex = int(((checkingChunkCoord.z * worldChunkLength + checkingChunkCoord.y) * worldChunkLength + checkingChunkCoord.x) * chunkVolume);
        vec3 checkingBlockLocalPos = floor(mod(checkingBlockPos, chunkLength));
        int checkingLocalBlockIndex = int((checkingBlockLocalPos.z * chunkLength + checkingBlockLocalPos.y) * chunkLength + checkingBlockLocalPos.x);
        int checkingBlockIndex = checkingChunkIndex + checkingLocalBlockIndex;
        if (chunks[checkingBlockIndex] != 0us)
            continue;

        for (int vert = 0; vert < 4; vert++)
        {
            gUV = uvOffsets[vert];
            gFace = f;
            gl_Position = projection * view * vec4(blockVertices[quads[4 * f + vert]] + blockPos, 1.0);
            EmitVertex();
        }
        EndPrimitive();
    }
}
