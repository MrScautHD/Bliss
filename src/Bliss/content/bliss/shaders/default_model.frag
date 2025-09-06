#version 450

#define MAX_MAPS_COUNT 8

layout(set = 2, binding = 0) uniform MaterialBuffer {
    int renderMode;
    vec4[MAX_MAPS_COUNT] colors;
    float[MAX_MAPS_COUNT] values;
};

layout (set = 3, binding = 0) uniform texture2D fAlbedo;
layout (set = 3, binding = 1) uniform sampler fAlbedoSampler;

layout (location = 0) in vec2 fTexCoords;
layout (location = 1) in vec2 fTexCoords2;
layout (location = 2) in vec3 fNormal;
layout (location = 3) in vec4 fTangent;
layout (location = 4) in vec4 fColor;

layout (location = 0) out vec4 fFragColor;

void main() {
    vec4 texelColor = texture(sampler2D(fAlbedo, fAlbedoSampler), fTexCoords);
    
    // Set render mode.
    switch (renderMode) {
            
        // Solid.
        case 0:
            texelColor.a = 1.0F;
            break;
            
        // Cutout.
        case 1:
            if (texelColor.a < 0.99F) {
                discard;
            }
            break;
    }
    
    fFragColor = texelColor * colors[0];
}