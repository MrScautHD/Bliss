/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: Bliss License 1.0
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

#version 450

layout (location = 0) in vec4 fColor;

layout (location = 0) out vec4 fFragColor;

void main() {
    fFragColor = fColor;
}