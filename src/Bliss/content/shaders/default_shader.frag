#version 450 core
layout(set = 0, binding = 0) uniform texture2D TestTexture;
layout(set = 0, binding = 1) uniform sampler TestSampler;

layout(location = 0) in vec2 TexCoord;
layout(location = 0) out vec4 FragColor;

void main() {
    FragColor = texture(sampler2D(TestTexture, TestSampler), TexCoord);
}