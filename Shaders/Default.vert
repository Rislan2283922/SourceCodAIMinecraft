#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aTexCoord; // 3D coordinates: U, V, Layer Index
layout (location = 2) in vec3 aColor;    // Biome tint + AO
layout (location = 3) in vec2 aLight;    // x = Sun Light (0-15), y = Block Light (0-15)

out vec3 texCoord;
out vec3 vertColor;
out vec2 lightLevels;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main() 
{
    gl_Position = vec4(aPosition, 1.0) * model * view * projection;
    texCoord = aTexCoord;
    vertColor = aColor;
    lightLevels = aLight;
}