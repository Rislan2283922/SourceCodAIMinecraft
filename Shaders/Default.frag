#version 330 core

in vec3 texCoord;
in vec3 vertColor;
in vec2 lightLevels; // x = Sun (0-15), y = Block (0-15)

out vec4 FragColor;

uniform sampler2DArray textureArray;

uniform vec3 fogColor;
uniform float fogDensity;
uniform vec4 overlayColor;
uniform vec3 globalLight; // x = Sun intensity
uniform int isCrack;
uniform float time; // <-- ADDED FOR ANIMATION

// Function to convert HSV to RGB (for rainbow effect)
vec3 hsv2rgb(vec3 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main()
{
    vec4 texColor = texture(textureArray, texCoord);

    // --- CRACK OVERLAY ---
    if (isCrack == 1)
    {
        float brightness = (texColor.r + texColor.g + texColor.b) / 3.0;
        if (brightness > 0.4) discard;
        texColor = vec4(0.0, 0.0, 0.0, 1.0);
    }
    else
    {
        if (texColor.a < 0.1) discard;
    }

    // --- RGB EFFECT CHECK ---
    // If the vertex color R channel is extremely high (set by C#), activate rainbow mode
    if (vertColor.r > 2.0 && isCrack == 0)
    {
        // Generate rainbow based on time and texture coordinates
        vec3 rainbow = hsv2rgb(vec3(time * 0.5 + texCoord.x + texCoord.y, 0.8, 1.0));
        
        // Blend texture with rainbow
        vec3 rgbFinal = texColor.rgb * rainbow;
        
        // Apply minimal fog but keep it glowing
        float dist = gl_FragCoord.z / gl_FragCoord.w;
        float fogFactor = exp(-pow(dist * fogDensity, 2.0));
        fogFactor = clamp(fogFactor, 0.0, 1.0);
        
        vec3 result = mix(fogColor, rgbFinal, fogFactor);
        
        FragColor = vec4(result, texColor.a);
        return; // Skip standard lighting
    }

    // --- REALISTIC LIGHTING ---
    
    float sunRaw = lightLevels.x / 15.0;
    float blockRaw = lightLevels.y / 15.0;

    // Sun depends on time of day
    float sunFinal = sunRaw * globalLight.x;

    // Mix lighting (take the brightest source)
    float lightAmt = max(sunFinal, blockRaw);

    // --- GAMMA / SOFTNESS ---
    float contrast = pow(lightAmt, 1.4);

    // --- AMBIENT ---
    float dayAmbient = 0.15 * globalLight.x; 
    float minAmbient = max(0.02, dayAmbient);

    // Final brightness
    float finalIntensity = max(contrast, minAmbient);

    if (isCrack == 1) finalIntensity = 1.0;

    // Apply lighting + biome color (AO)
    vec3 lighting = vertColor * vec3(finalIntensity);
    vec3 finalObjColor = texColor.rgb * lighting;

    // Damage overlay
    finalObjColor = mix(finalObjColor, overlayColor.rgb, overlayColor.a);

    // Fog
    float dist = gl_FragCoord.z / gl_FragCoord.w;
    float fogFactor = exp(-pow(dist * fogDensity, 2.0));
    fogFactor = clamp(fogFactor, 0.0, 1.0);

    vec3 result = mix(fogColor, finalObjColor, fogFactor);

    FragColor = vec4(result, texColor.a);
}