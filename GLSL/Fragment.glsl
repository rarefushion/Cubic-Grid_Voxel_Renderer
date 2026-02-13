#version 430 core

in flat int gBlock;
in vec2 gUV;
in flat int gFace;

out vec4 FragColor;

uniform sampler2DArray textureArray;
uniform float[512] textureIDs;

void main()
{
    FragColor = texture(textureArray, vec3(gUV, textureIDs[gBlock * 6 + gFace]));
}