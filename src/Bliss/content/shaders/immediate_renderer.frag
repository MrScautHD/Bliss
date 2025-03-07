#version 450

layout (set = 1, binding = 0) uniform texture2D fTexture;
layout (set = 1, binding = 1) uniform sampler fTextureSampler;

layout (location = 0) in vec2 fTexCoords;
layout (location = 1) in vec4 fColor;

layout (location = 0) out vec4 fFragColor;

void main() {
    vec4 texelColor = texture(sampler2D(fTexture, fTextureSampler), fTexCoords);

    if (texelColor.a <= 0.0F) {
        discard;
    }

    fFragColor = texelColor * fColor;
}