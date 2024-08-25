#version 450

// Input from vertex shader.
layout(location = 0) in vec2 fragTexCoord;
layout(location = 1) in vec4 fragColor;

// Input unifrom values.
layout(set = 0, binding = 0) uniform sampler2D texture0;
layout(set = 0, binding = 1) uniform Color {
    vec4 colDiffuse;
};

// Output fragment color.
layout(location = 0) out vec4 finalColor;

void main() {
    vec4 texelColor = texture(texture0, fragTexCoord);
    finalColor = texelColor * colDiffuse * fragColor;
}