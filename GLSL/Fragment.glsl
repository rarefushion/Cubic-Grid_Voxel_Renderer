#version 330 core

in float gBlock;
in vec2 gUV;
in float gFace;

out vec4 FragColor;

uniform sampler2DArray textureArray;
uniform float[512] textureIDs;

void main()
{
    FragColor = texture(textureArray, vec3(gUV, textureIDs[(int(gBlock + 0.5) * 6) + int(gFace + 0.5)]));
}