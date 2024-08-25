#version 450

layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 TexCoord;

layout(set = 0, binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};

layout(location = 0) out vec2 fsin_texCoord;

void main()
{
    gl_Position = Projection * vec4(Position, 0.0, 1.0);
    fsin_texCoord = TexCoord;
}