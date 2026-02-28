#version 430 core

out flat int vBlockIndex;

void main()
{
    vBlockIndex = gl_VertexID;
}
