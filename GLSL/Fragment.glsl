#version 430 core

in flat int vBlock;
in vec2 vUV;
in flat int vFace;
in float vBrightness;

out vec4 FragColor;

uniform sampler2DArray textureArray;
layout(binding=1) buffer TextureIDBuffer { flat float textureIDs[]; };

void main()
{
    FragColor = texture(textureArray, vec3(vUV, textureIDs[vBlock * 6 + vFace])) * vBrightness;
}