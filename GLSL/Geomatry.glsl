#version 330 core
layout (points) in;
layout (triangle_strip, max_vertices = 24) out;

in float vBlock[];
in vec3 vWorldPos[];

out float gBlock;
out vec2 gUV;
out float gFace;

uniform mat4 projection;
uniform mat4 view;

bool WholeNumEqual(float x, float y)
{
    const float eps = 0.1;
    return abs(x - y) < eps;
}

void main()
{
    // Constants
    int vertIndexForTris[6] = int[](0, 1, 2, 2, 1, 3);
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
    // Create Faces
    if (WholeNumEqual(vBlock[0], 0.0)) return;
    gBlock = vBlock[0];
    for (int f = 0; f < 6; f++)
    {
        for (int vert = 0; vert < 4; vert++)
        {
            gUV = uvOffsets[vert];
            gFace = float(f);
            vec3 pos = blockVertices[quads[4 * f + vert]] + vWorldPos[0];
            gl_Position = projection * view * vec4(pos, 1.0);
            EmitVertex();
        }
        EndPrimitive();
    }
}