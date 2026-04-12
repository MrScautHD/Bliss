#version 450

#define MAX_BONE_COUNT 72

layout(std140, set = 0, binding = 0) uniform MatrixBuffer {
    mat4x4 uProjection;
    mat4x4 uView;
};

layout(std140, set = 1, binding = 0) uniform TransformBuffer {
    mat4x4 uTransformation;
};

layout(std140, set = 2, binding = 0) uniform BoneBuffer {
    mat4x4 uBonesTransformations[MAX_BONE_COUNT];
};

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec4 vBoneWeights;
layout (location = 2) in uvec4 vBoneIndices;
layout (location = 3) in vec2 vTexCoords;
layout (location = 4) in vec2 vTexCoords2;
layout (location = 5) in vec3 vNormal;
layout (location = 6) in vec4 vTangent;
layout (location = 7) in vec4 vColor;

#if USE_INSTANCING
layout (location = 8) in vec4 iModel0;
layout (location = 9) in vec4 iModel1;
layout (location = 10) in vec4 iModel2;
layout (location = 11) in vec4 iModel3;
#endif

layout (location = 0) out vec2 fTexCoords;

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

    #if USE_INSTANCING
    mat4x4 transformation = mat4x4(iModel0, iModel1, iModel2, iModel3);
    #else
    mat4x4 transformation = uTransformation;
    #endif

    mat4x4 boneTransformation = getBoneTransformation();

    vec4 v4Pos = vec4(vPosition, 1.0F);
    gl_Position = uProjection * uView * transformation * boneTransformation * v4Pos;
}