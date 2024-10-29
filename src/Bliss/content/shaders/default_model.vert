#version 450

layout(location = 0) in vec3 inPosition;   // Vertex position
layout(location = 1) in vec3 inNormal;     // Vertex normal
layout(location = 2) in vec2 inTexCoords;  // Texture coordinates

layout(location = 0) out vec2 fragTexCoords;  // Passed to fragment shader
layout(location = 1) out vec3 fragPos;        // Position in world space
layout(location = 2) out vec3 fragNormal;     // Normal in world space

layout(set = 0, binding = 0) uniform Matrices {
    mat4 model;
    mat4 viewProjection;
};

void main() {
    fragPos = vec3(model * vec4(inPosition, 1.0));
    fragNormal = mat3(transpose(inverse(model))) * inNormal;
    fragTexCoords = inTexCoords;
    gl_Position = viewProjection * vec4(fragPos, 1.0);
}