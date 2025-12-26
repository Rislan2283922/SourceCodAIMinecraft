#version 330 core
out vec4 FragColor;

in vec3 TexCoord; // u, v, layer

// We support both types. Uniform 'useArray' toggles between them.
uniform sampler2D uiTexture;
uniform sampler2DArray uiTextureArray;
uniform int useArray; // 1 = use array, 0 = use 2D

uniform vec3 colorTint;
uniform float alpha;

void main()
{
    vec4 texColor;
    
    if (useArray == 1)
    {
        texColor = texture(uiTextureArray, TexCoord);
    }
    else
    {
        // For 2D texture, we ignore the 3rd component (layer)
        texColor = texture(uiTexture, TexCoord.xy);
    }

    if(texColor.a < 0.05) discard;
    FragColor = texColor * vec4(colorTint, alpha);
}