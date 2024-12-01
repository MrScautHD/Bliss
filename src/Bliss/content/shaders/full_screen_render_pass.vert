/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

#version 450

layout (location = 0) in vec2 vPosition;
layout (location = 1) in vec2 vTexCoords;
layout (location = 2) in vec4 vColor;

layout (location = 0) out vec2 fTexCoords;
layout (location = 1) out vec4 fColor;

void main() {
    fTexCoords = vTexCoords;
    fColor = vColor;

    gl_Position = vec4(vPosition, 0.0, 1.0);
}