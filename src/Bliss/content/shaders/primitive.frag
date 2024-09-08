#version 450

layout (location = 1) in vec4 fColor;

layout (location = 0) out vec4 fFragColor;

void main() {
    fFragColor = fColor;
}