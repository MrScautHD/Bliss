#version 450

layout(set = 0, binding = 0) uniform MatrixBuffer {
    mat4 uProjection;
    mat4 uView;
    mat4 uTransformation;
};

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec2 vTexCoords;
layout (location = 2) in vec2 vTexCoords2;
layout (location = 3) in vec3 vNormal;
layout (location = 4) in vec3 vTangent;
layout (location = 5) in vec4 vColor;

layout (location = 0) out vec2 fTexCoords;
layout (location = 1) out vec2 fTexCoords2;
layout (location = 2) out vec3 fNormal;
layout (location = 3) out vec3 fTangent;
layout (location = 4) out vec4 fColor;

void main() {
    fTexCoords = vTexCoords;
    fTexCoords2 = vTexCoords2;
    fNormal = vNormal;
    fTangent = vTangent;
    fColor = vColor;

    vec4 v4Pos = vec4(vPosition, 1.0F);
    gl_Position = uProjection * uView * uTransformation * v4Pos;
}