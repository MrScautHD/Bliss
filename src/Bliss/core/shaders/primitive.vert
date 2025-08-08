#version 450

layout (set = 0, binding = 0) uniform ProjectionViewBuffer {
    mat4x4 uProj;
    mat4x4 uView;
};

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec4 vColor;

layout (location = 0) out vec4 fColor;

void main() {
    fColor = vColor;

    gl_Position = uProj * uView * vec4(vPosition.xy, clamp(vPosition.z, 0.0F, 1.0F), 1.0F);
}