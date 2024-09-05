#version 450

layout(set = 0, binding = 0) uniform ProjectionViewBuffer {
    mat4x4 uProjView;
};

layout (location = 0) in vec2 vPosition;
layout (location = 1) in vec2 vTexCoords;
layout (location = 2) in vec4 vColor;

layout (location = 0) out vec2 fTexCoords;
layout (location = 1) out vec4 fColor;

void main() {
    fTexCoords = vTexCoords;
    fColor = vColor;
    
    gl_Position = uProjView * vec4(vPosition.x, vPosition.y, 0.0, 1.0);
}