/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

#version 450

layout (set = 0, binding = 0) uniform ProjectionViewBuffer {
    mat4x4 uProj;
    mat4x4 uView;
};

layout (location = 0) in vec2 vPosition;
layout (location = 1) in vec4 vColor;

layout (location = 0) out vec4 fColor;

void main() {
    fColor = vColor;

    gl_Position = uProj * uView * vec4(vPosition, 0.0, 1.0);
}