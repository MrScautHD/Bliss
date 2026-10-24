#version 450

layout (std140, set = 0, binding = 0) uniform MatrixBuffer {
    mat4x4 uProjection;
    mat4x4 uView;
};

layout (std140, set = 1, binding = 0) uniform SkyboxBuffer {
    mat4x4 uRotation;
    vec4 uColor;
};

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec4 vColor;

layout (location = 0) out vec3 fTexCoords;
layout (location = 1) out vec4 fColor;

void main() {
    vec3 rotatedDirection = mat3(uRotation) * vPosition;
    
    fTexCoords = rotatedDirection;
    fColor = vColor;
    
    mat4 viewWithoutTranslation = mat4(mat3(uView));
    vec4 position = uProjection * viewWithoutTranslation * vec4(vPosition, 1.0F);
    
    gl_Position = position.xyww;
}