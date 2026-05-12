#version 430 core

in flat int vBlock;
in centroid vec2 vUV;
in flat int vFace;
in float vBrightness;

out vec4 FragColor;

uniform sampler2DArray textureArray;
layout(binding=1) buffer TextureIDBuffer { flat float textureIDs[]; };

void main()
{
    vec4 textureColor = texture(textureArray, vec3(vUV, textureIDs[vBlock * 6 + vFace]));
    if (textureColor.a < 0.01)
        discard;
    FragColor = vec4(textureColor.rgb * vBrightness, textureColor.a);
}