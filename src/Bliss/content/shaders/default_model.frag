#version 450

layout(location = 0) in vec2 fragTexCoords;
layout(location = 1) in vec3 fragPos;
layout(location = 2) in vec3 fragNormal;

layout(location = 0) out vec4 outColor;

// Material textures
layout(set = 1, binding = 0) uniform sampler2D fAlbedoTexture;
layout(set = 1, binding = 1) uniform sampler2D fMetallicTexture;
layout(set = 1, binding = 2) uniform sampler2D fRoughnessTexture;
layout(set = 1, binding = 3) uniform sampler2D fNormalTexture;
layout(set = 1, binding = 4) uniform sampler2D fEmissionTexture;

// Light and camera information
layout(set = 2, binding = 0) uniform Light {
    vec3 lightPos;
    vec3 lightColor;
};

layout(set = 2, binding = 1) uniform Camera {
    vec3 viewPos;
};

const float PI = 3.14159265359;

// Helper function to calculate normal distribution function for PBR
float DistributionGGX(vec3 N, vec3 H, float roughness) {
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;

    float num = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return num / denom;
}

// Fresnel-Schlick approximation for PBR specular reflection
vec3 FresnelSchlick(float cosTheta, vec3 F0) {
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

// Geometry function for PBR shadowing and masking
float GeometrySchlickGGX(float NdotV, float roughness) {
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;

    float num = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return num / denom;
}

// Combined geometry function
float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness) {
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

void main() {
    // Material properties
    vec3 albedo = texture(fAlbedoTexture, fragTexCoords).rgb;
    float metallic = texture(fMetallicTexture, fragTexCoords).r;
    float roughness = texture(fRoughnessTexture, fragTexCoords).r;
    vec3 emission = texture(fEmissionTexture, fragTexCoords).rgb;

    // Lighting calculations
    vec3 N = normalize(fragNormal);
    vec3 V = normalize(viewPos - fragPos);
    vec3 L = normalize(lightPos - fragPos);
    vec3 H = normalize(V + L);

    // Reflection coefficients
    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo, metallic);

    // Calculate the reflectance equations
    float NDF = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

    vec3 nominator = NDF * G * F;
    float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.001;
    vec3 specular = nominator / denominator;

    // Diffuse component
    vec3 kD = vec3(1.0) - F;
    kD *= 1.0 - metallic;
    vec3 diffuse = kD * albedo / PI;

    // Combine diffuse and specular with light intensity
    vec3 Lo = (diffuse + specular) * lightColor * max(dot(N, L), 0.0);

    // Final output color with emission
    vec3 color = Lo + emission;
    outColor = vec4(color, 1.0);
}