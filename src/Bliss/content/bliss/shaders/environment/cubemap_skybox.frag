#version 450

layout (std140, set = 1, binding = 0) uniform SkyboxBuffer {
    mat4x4 uRotation;
    vec4 uColor;
};

layout (set = 2, binding = 0) uniform textureCube fCubemap;
layout (set = 2, binding = 1) uniform sampler fCubemapSampler;

layout (location = 0) in vec3 fTexCoords;
layout (location = 1) in vec4 fColor;

layout (location = 0) out vec4 fFragColor;

void main() {
    vec4 texelColor = texture(samplerCube(fCubemap, fCubemapSampler), normalize(fTexCoords));
    vec3 finalColor = texelColor.rgb * uColor.rgb * uColor.a * fColor.rgb;
    
    fFragColor = vec4(finalColor, texelColor.a * fColor.a);
}