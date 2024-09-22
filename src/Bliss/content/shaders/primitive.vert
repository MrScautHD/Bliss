#version 450

layout (set = 0, binding = 0) uniform ProjectionViewBuffer {
    mat4x4 uProjView;
};

layout (location = 0) in vec2 vPosition;
layout (location = 1) in vec4 vColor;

layout (location = 0) out vec4 fColor;

void main() {
    fColor = vColor;

    //mat4 _ProjTest = mat4(0.0125, 0, 0, 0, 0, 0.0222222, 0,0,0,0, -0.01, 0,0,0,0,1);
    gl_Position = uProjView * vec4(vPosition, 0.0, 1.0);
}