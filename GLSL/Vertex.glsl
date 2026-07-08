#version 430 core
#if defined(GL_NV_gpu_shader5)
    #extension GL_NV_gpu_shader5 : enable
#elif defined(GL_EXT_shader_explicit_arithmetic_types_int16)
    #extension GL_EXT_shader_explicit_arithmetic_types_int16 : enable
#endif
#extension GL_ARB_shader_draw_parameters : require

struct ShapeVertex
{
    vec3 position;
    float _pad0;
    vec2 uv;
    float _pad2;
    float _pad1;
};

layout(binding=3) buffer ShapeVertices { ShapeVertex[] shapeVertices; };

struct ShapeInstance
{
    float posX, posY, posZ;
    int texture;
    uint16_t shape;
    uint16_t rotation;
    float tintR, tintG, tintB;
};

layout(binding=0) buffer ShapeInstances { ShapeInstance[] shapeInstances; };

out vec2 vUV;
out flat int vTexture;
out vec3 vTint;

uniform int verticesPerShape;

uniform vec3 chunkPos;

uniform mat4 projection;
uniform mat4 view;

const mat3 ROTATION[24] = mat3[24]
(
    // up=-Z
    mat3(vec3( 1,  0,  0), vec3( 0,  0, -1), vec3( 0,  1,  0)),  //  0: fwd=+Y
    mat3(vec3( 0,  1,  0), vec3( 0,  0, -1), vec3(-1,  0,  0)),  //  1: fwd=-X
    mat3(vec3(-1,  0,  0), vec3( 0,  0, -1), vec3( 0, -1,  0)),  //  2: fwd=-Y
    mat3(vec3( 0, -1,  0), vec3( 0,  0, -1), vec3( 1,  0,  0)),  //  3: fwd=+X
    // up=+Z
    mat3(vec3( 1,  0,  0), vec3( 0,  0,  1), vec3( 0, -1,  0)),  //  4: fwd=-Y
    mat3(vec3( 0, -1,  0), vec3( 0,  0,  1), vec3(-1,  0,  0)),  //  5: fwd=-X
    mat3(vec3(-1,  0,  0), vec3( 0,  0,  1), vec3( 0,  1,  0)),  //  6: fwd=+Y
    mat3(vec3( 0,  1,  0), vec3( 0,  0,  1), vec3( 1,  0,  0)),  //  7: fwd=+X
    // up=+Y
    mat3(vec3( 1,  0,  0), vec3( 0,  1,  0), vec3( 0,  0,  1)),  //  8: fwd=+Z
    mat3(vec3( 0,  0,  1), vec3( 0,  1,  0), vec3(-1,  0,  0)),  //  9: fwd=-X
    mat3(vec3(-1,  0,  0), vec3( 0,  1,  0), vec3( 0,  0, -1)),  // 10: fwd=-Z
    mat3(vec3( 0,  0, -1), vec3( 0,  1,  0), vec3( 1,  0,  0)),  // 11: fwd=+X
    // up=-Y
    mat3(vec3(-1,  0,  0), vec3( 0, -1,  0), vec3( 0,  0,  1)),  // 12: fwd=+Z
    mat3(vec3( 0,  0, -1), vec3( 0, -1,  0), vec3(-1,  0,  0)),  // 13: fwd=-X
    mat3(vec3( 1,  0,  0), vec3( 0, -1,  0), vec3( 0,  0, -1)),  // 14: fwd=-Z
    mat3(vec3( 0,  0,  1), vec3( 0, -1,  0), vec3( 1,  0,  0)),  // 15: fwd=+X
    // up=-X
    mat3(vec3( 0,  0,  1), vec3(-1,  0,  0), vec3( 0, -1,  0)),  // 16: fwd=-Y
    mat3(vec3( 0,  1,  0), vec3(-1,  0,  0), vec3( 0,  0,  1)),  // 17: fwd=+Z
    mat3(vec3( 0,  0, -1), vec3(-1,  0,  0), vec3( 0,  1,  0)),  // 18: fwd=+Y
    mat3(vec3( 0, -1,  0), vec3(-1,  0,  0), vec3( 0,  0, -1)),  // 19: fwd=-Z
    // up=+X
    mat3(vec3( 0,  0,  1), vec3( 1,  0,  0), vec3( 0,  1,  0)),  // 20: fwd=+Y
    mat3(vec3( 0,  1,  0), vec3( 1,  0,  0), vec3( 0,  0, -1)),  // 21: fwd=-Z
    mat3(vec3( 0,  0, -1), vec3( 1,  0,  0), vec3( 0, -1,  0)),  // 22: fwd=-Y
    mat3(vec3( 0, -1,  0), vec3( 1,  0,  0), vec3( 0,  0,  1))   // 23: fwd=+Z
);

void main()
{
    ShapeInstance instance = shapeInstances[gl_BaseInstanceARB + gl_InstanceID];
    ShapeVertex vert = shapeVertices[gl_VertexID + int(instance.shape) * verticesPerShape];

    vUV = vert.uv;
    vTexture = instance.texture;
    vTint = vec3(instance.tintR, instance.tintG, instance.tintB);

    vec3 rotatedVert = ROTATION[int(instance.rotation)] * (vert.position - 0.5) + 0.5;
    vec3 instancePos = vec3(instance.posX, instance.posY, instance.posZ);
    gl_Position = projection * view * vec4(rotatedVert + chunkPos + instancePos, 1.0);
}
