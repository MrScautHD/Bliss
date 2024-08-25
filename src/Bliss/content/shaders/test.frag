#version 450

layout(set = 0, binding = 1) uniform sampler2D TextureSampler;
layout(location = 0) in vec2 fsin_texCoord;
layout(location = 0) out vec4 fsout_color;

void main()
{
    fsout_color = texture(TextureSampler, fsin_texCoord);
}