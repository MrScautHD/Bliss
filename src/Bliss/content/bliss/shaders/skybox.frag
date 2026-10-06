#version 450

layout (set = 1, binding = 0) uniform textureCube fCubemap;
layout (set = 1, binding = 1) uniform sampler fCubemapSampler;

layout (location = 0) in vec3 fTexCoords;
layout (location = 1) in vec4 fColor;

layout (location = 0) out vec4 fFragColor;

void main() {
    fFragColor = texture(samplerCube(fCubemap, fCubemapSampler), fTexCoords) * fColor;
}