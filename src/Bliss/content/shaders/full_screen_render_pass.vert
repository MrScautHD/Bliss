#version 450
layout(location = 0) in vec4 vPosition;

layout(location = 0) out vec2 vTexCoords;

void main() {
    vTexCoords = vPosition.zw;

    gl_Position = vec4(vPosition.xy, 0, 1);
}