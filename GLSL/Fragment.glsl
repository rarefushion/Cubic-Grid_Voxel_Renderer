#version 430 core

in centroid vec2 vUV;
in flat int vTexture;
in vec3 vTint;

out vec4 FragColor;

uniform sampler2DArray textureArray;

void main()
{
    vec4 textureColor = texture(textureArray, vec3(vUV, vTexture));
    if (textureColor.a < 0.01)
        discard;
    FragColor = vec4(textureColor.rgb * vTint, textureColor.a);
}