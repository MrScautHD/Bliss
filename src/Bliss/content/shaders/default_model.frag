#version 450

#define MAX_MAPS_COUNT 7

layout(set = 2, binding = 0) uniform ColorBuffer {
    vec4[MAX_MAPS_COUNT] uColors;
};

layout(set = 3, binding = 0) uniform ValueBuffer {
    float[MAX_MAPS_COUNT] uValues;
};

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

    if (texelColor.a <= 0.0F) {
        discard;
    }

    fFragColor = texelColor * uColors[0];
}