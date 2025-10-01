#version 450

#define MAX_MAPS_COUNT 8

layout(std140, set = 2, binding = 0) uniform MaterialBuffer {
    int renderMode;
    vec4[MAX_MAPS_COUNT] colors; // TODO: Rework struct managment.
    float[MAX_MAPS_COUNT] values;
};

// TODO: Remove it by default!
struct Light {
    int type; // Type: (Direction = 0, Point = 1, Spot = 2).
    int id; // The id to identify the light with.
    float range; // Range.
    float spotAngle; // SpotAngle.
    vec4 position; // xyz: (Position), w: (Padding).
    vec4 direction; // xyz: (Direction), w: (Padding).
    vec4 color; // rgb: (Color), w: (Intensity).
};

layout(std430, set = 3, binding = 0) buffer LightBuffer {
    int numOfLights; // Number of lights.
    vec4 ambientColor; // rgb: (Color), w: (Intensity).
    Light[] lights; // The lights array.
};

//layout(std140, set = 3, binding = 0) uniform LightBuffer {
//    int numOfLights; // Number of lights.
//    vec4 ambientColor; // rgb: (Color), w: (Intensity).
//    Light[512] lights; // The lights array.
//};

layout (set = 4, binding = 0) uniform texture2D fAlbedo;
layout (set = 4, binding = 1) uniform sampler fAlbedoSampler;

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
    
    for (int i = 0; i < numOfLights; i++) {
        //if (lights[i].type == 1) { // Point light.
        //    texelColor = vec4(1, 1, 1, 1);
        //}
        
        texelColor = lights[i].color;
    }
    
    fFragColor = texelColor * colors[0];
}