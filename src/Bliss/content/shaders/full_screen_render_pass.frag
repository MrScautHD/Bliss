/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

#version 450

layout (set = 0, binding = 0) uniform texture2D fTexture;
layout (set = 0, binding = 1) uniform sampler fTextureSampler;

layout (location = 0) in vec2 fTexCoords;
layout (location = 1) in vec4 fColor;

layout (location = 0) out vec4 fFragColor;

void main() {
    fFragColor = texture(sampler2D(fTexture, fTextureSampler), fTexCoords) * fColor;
}