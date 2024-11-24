#version 450

// TODO: Try todo it better maybe some system that just adding them by set locations (i think it even crash with vulkan, because vulkan in vulkan the amount of sampler2Ds and the TextureLayouts need to be the same (match))
layout (set = 1, binding = 0) uniform texture2D fAlbedoTexture;
layout (set = 1, binding = 1) uniform sampler fAlbedoTextureSampler;

layout (set = 2, binding = 2) uniform texture2D fMetallicTexture;
layout (set = 2, binding = 3) uniform sampler fMetallicTextureSampler;

layout (set = 3, binding = 4) uniform texture2D fNormalTexture;
layout (set = 3, binding = 5) uniform sampler fNormalTextureSampler;

layout (set = 4, binding = 6) uniform texture2D fRoughnessTexture;
layout (set = 4, binding = 7) uniform sampler fRoughnessTextureSampler;

layout (set = 5, binding = 8) uniform texture2D fOcclusionTexture;
layout (set = 5, binding = 9) uniform sampler fOcclusionTextureSampler;

layout (set = 6, binding = 10) uniform texture2D fEmissionTexture;
layout (set = 6, binding = 11) uniform sampler fEmissionTextureSampler;

layout (set = 7, binding = 12) uniform texture2D fHeightTexture;
layout (set = 7, binding = 13) uniform sampler fHeightTextureSampler;

layout (location = 0) in vec2 fTexCoords;
layout (location = 1) in vec2 fTexCoords2;
layout (location = 2) in vec3 fNormal;
layout (location = 3) in vec3 fTangent;
layout (location = 4) in vec4 fColor;

layout (location = 0) out vec4 fFragColor;

void main() {
    vec4 texelColor = texture(sampler2D(fAlbedoTexture, fAlbedoTextureSampler), fTexCoords);

    if (texelColor.a <= 0.0) {
        discard;
    }

    fFragColor = texelColor;
}