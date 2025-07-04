#version 450

layout (set = 1, binding = 0) uniform texture2D fTexture;
layout (set = 1, binding = 1) uniform sampler fTextureSampler;

layout (location = 0) in vec2 fTexCoords;
layout (location = 1) in vec4 fColor;
layout (location = 2) in vec4 fLineData;

layout (location = 0) out vec4 fFragColor;

void main() {
    vec4 texelColor = texture(sampler2D(fTexture, fTextureSampler), fTexCoords);

    if (texelColor.a <= 0.0F) {
        discard;
    }

    vec4 color = texelColor * fColor;

    if (fLineData.w > 0.01) {
        float stipplePattern = (fLineData.x / fLineData.z) * fLineData.y;
        stipplePattern += 0.25f;
        if (fract(stipplePattern) > 0.5) {
            discard;
        }
    }
    fFragColor = color;
}