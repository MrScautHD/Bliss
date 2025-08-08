#version 450

layout(set = 0, binding = 0) uniform MatrixBuffer {
    mat4x4 uProjection;
    mat4x4 uView;
    mat4x4 uTransformation;
};

layout(set = 1, binding = 0) uniform BoneBuffer {
    mat4x4 uBonesTransformations[128];
};

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec4 vBoneWeights;
layout (location = 2) in uvec4 vBoneIndices;
layout (location = 3) in vec2 vTexCoords;
layout (location = 4) in vec2 vTexCoords2;
layout (location = 5) in vec3 vNormal;
layout (location = 6) in vec4 vTangent;
layout (location = 7) in vec4 vColor;

layout (location = 0) out vec2 fTexCoords;
layout (location = 1) out vec2 fTexCoords2;
layout (location = 2) out vec3 fNormal;
layout (location = 3) out vec4 fTangent;
layout (location = 4) out vec4 fColor;

mat4x4 getBoneTransformation() {
    if (length(vBoneWeights) == 0.0F) {
        return mat4x4(1.0F);
    }
    
    mat4x4 boneTransformation = uBonesTransformations[vBoneIndices.x] * vBoneWeights.x;
    boneTransformation += uBonesTransformations[vBoneIndices.y] * vBoneWeights.y;
    boneTransformation += uBonesTransformations[vBoneIndices.z] * vBoneWeights.z;
    boneTransformation += uBonesTransformations[vBoneIndices.w] * vBoneWeights.w;
    
    return boneTransformation;
}

void main() {
    fTexCoords = vTexCoords;
    fTexCoords2 = vTexCoords2;
    fNormal = vNormal;
    fTangent = vTangent;
    fColor = vColor;

    mat4x4 boneTransformation = getBoneTransformation();
    vec4 v4Pos = vec4(vPosition, 1.0F);
    gl_Position = uProjection * uView * uTransformation * boneTransformation * v4Pos;
}