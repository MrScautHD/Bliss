#version 450

layout (std140, set = 1, binding = 0) uniform SkyboxBuffer {
    vec4 uSkyColor; // rgb: (Sky tint that is mixed into the gradient), a: (Unused).
    vec4 uGroundColor; // rgb: (Color of the ground below the horizon), a: (Unused).
    vec4 uSunColor; // rgb: (Color of the sun disc and its glow), a: (Unused).
    vec4 uColor; // rgb: (Tint multiplied with the whole sky), a: (Opacity).
    vec4 uSunDirection; // xyz: (Direction towards the sun, normalized in the shader), w: (Unused).
    float uSunSize; // Size of the sun disc as a cosine margin. The disc covers all directions with dot(direction, sun) > 1 - size.
    float uSunSizeConvergence; // How tightly the glow hugs the sun, clamped to [0, 10]. Higher values give a tighter glow.
    float uAtmosphereThickness; // Shapes the sky gradient, clamped to [0, 5]. Higher values push the pale horizon color higher into the sky.
    float uExposure; // Brightness multiplier. 1.0 leaves the sky unchanged.
    int uSunQuality; // Sun rendering: (Off = 0, Simple disc = 1, Disc with glow = 2 or higher).
};

layout (location = 0) in vec3 fDirection;
layout (location = 1) in vec4 fColor;

layout (location = 0) out vec4 fFragColor;

/*
 * Converts a color from display space (gamma 2.2) to linear space.
 *
 * Mixing and lighting are only correct in linear space. Negative values are clamped to zero.
 */
vec3 gammaToLinear(vec3 color) {
    return pow(max(color, vec3(0.0F)), vec3(2.2F));
}

/*
 * Converts a color from linear space to display space (gamma 2.2).
 *
 * Negative values are clamped to zero.
 */
vec3 linearToGamma(vec3 color) {
    return pow(max(color, vec3(0.0F)), vec3(1.0F / 2.2F));
}

/*
 * Compresses an HDR color into the range [0, 1] using the ACES filmic curve (Narkowicz fit).
 *
 * Compared to a simple Reinhard curve, the result has more contrast and keeps colors more saturated.
 */
vec3 tonemapAces(vec3 color) {
    const float a = 2.51F;
    const float b = 0.03F;
    const float c = 2.43F;
    const float d = 0.59F;
    const float e = 0.14F;
    
    return clamp((color * (a * color + b)) / (color * (c * color + d) + e), 0.0F, 1.0F);
}

/*
 * Maps a value from the range [minValue, maxValue] linearly to [0, 1] and clamps it.
 */
float remap01(float value, float minValue, float maxValue) {
    return clamp((value - minValue) / (maxValue - minValue), 0.0F, 1.0F);
}

void main() {
    vec3 direction = normalize(fDirection);
    vec3 sunDirection = normalize(uSunDirection.xyz);
    
    // The height is the vertical component of the view direction: 1 straight up, 0 at the horizon, -1 straight down.
    float height = direction.y;
    
    // 1.0 when looking directly at the sun. The sun is only drawn in the hemisphere it is in, so negative values are clamped.
    float sunDot = clamp(dot(direction, sunDirection), 0.0F, 1.0F);
    
    // Limit the settings to the range the formulas below are designed for.
    float atmosphere = clamp(uAtmosphereThickness, 0.0F, 5.0F);
    float atmosphere01 = atmosphere / 5.0F;
    
    float exposure = max(uExposure, 0.0F);
    float sunSize = clamp(uSunSize, 0.0001F, 0.5F);
    float sunConvergence = clamp(uSunSizeConvergence, 0.0F, 10.0F);
    
    // Colors are mixed and multiplied in linear space.
    vec3 skyTint = gammaToLinear(uSkyColor.rgb);
    vec3 groundColor = gammaToLinear(uGroundColor.rgb);
    vec3 sunColor = gammaToLinear(uSunColor.rgb);
    vec3 tintColor = gammaToLinear(uColor.rgb);
    
    // How far the direction is into the sky. The range starts slightly below the horizon, so the gradient continues a bit under the horizon line.
    float skyAmount = remap01(height, -0.08F, 1.0F);
    
    // The three base colors of the gradient: dark blue at the zenith, a medium blue in between, and a pale blue at the horizon.
    vec3 zenithBase = vec3(0.035F, 0.145F, 0.42F);
    vec3 upperBase = vec3(0.20F, 0.47F, 0.92F);
    vec3 horizonBase = vec3(0.78F, 0.88F, 1.0F);
    
    // The sky tint is mixed in with a different strength per color, so the zenith follows the tint most and the horizon least.
    vec3 zenithColor = mix(zenithBase, skyTint, 0.55F);
    vec3 upperSkyColor = mix(upperBase, skyTint, 0.45F);
    vec3 horizonSkyColor = mix(horizonBase, skyTint, 0.30F);
    
    // A higher exponent keeps the value low for longer, so a thicker atmosphere stretches the pale horizon color higher up.
    float skyCurve = pow(skyAmount, mix(0.75F, 1.85F, atmosphere01));
    
    // Blend the gradient: horizon to upper sky first, then upper sky to zenith.
    vec3 sky = mix(horizonSkyColor, upperSkyColor, smoothstep(0.0F, 0.55F, skyCurve));
    sky = mix(sky, zenithColor, smoothstep(0.45F, 1.0F, skyCurve));
    
    // A soft haze band around the horizon and a thin line directly on it. Both are symmetric, so they also lighten the ground side.
    float horizonBand = 1.0F - smoothstep(0.0F, 0.13F, abs(height));
    float horizonLine = 1.0F - smoothstep(0.0F, 0.025F, abs(height));
    
    // The haze is mostly white-blue, with a little of the sky tint. A thicker atmosphere makes the haze stronger.
    vec3 horizonHazeColor = mix(vec3(0.82F, 0.90F, 1.0F), skyTint, 0.18F);
    sky = mix(sky, horizonHazeColor, horizonBand * (0.28F + atmosphere01 * 0.22F));
    
    // The ground gets darker the further down it is, and fades into the haze towards the horizon.
    vec3 ground = groundColor;
    ground = mix(ground * 0.55F, ground, remap01(height, -1.0F, 0.0F));
    ground = mix(ground, horizonHazeColor * 0.50F, horizonBand * 0.35F);
    
    // A narrow smooth transition from sky to ground right at the horizon.
    float groundAmount = smoothstep(0.015F, -0.035F, height);
    
    vec3 finalColor = mix(sky, ground, groundAmount);
    finalColor = mix(finalColor, horizonHazeColor, horizonLine * 0.20F);
    
    // Simple sun: only a bright disc. The edge fades over the outer margin of the disc.
    if (uSunQuality == 1) {
        float disk = smoothstep(1.0F - sunSize, 1.0F, sunDot);
        finalColor += sunColor * disk * 4.0F;
    }
    // High quality sun: a brighter disc and three glow layers of different widths around it.
    else if (uSunQuality == 2) {
        float disk = smoothstep(1.0F - sunSize, 1.0F, sunDot);
        
        // A higher convergence raises the exponents, which shrinks every glow layer towards the sun.
        float convergence = sunConvergence / 10.0F;
        
        // The glow falls off with the angle to the sun. The medium and wide layers scale with the sun size, so a larger sun has a stronger glow.
        float tightPower = mix(64.0F, 2048.0F, convergence);
        float mediumPower = mix(32.0F, 256.0F, convergence);
        float widePower = mix(8.0F, 48.0F, convergence);
        
        float tightGlow = pow(sunDot, tightPower);
        float mediumGlow = pow(sunDot, mediumPower) * sunSize * 5.0F;
        float wideGlow = pow(sunDot, widePower) * sunSize * 0.35F;
        
        finalColor += sunColor * disk * 8.0F;
        finalColor += sunColor * tightGlow * 2.5F;
        finalColor += sunColor * mediumGlow;
        finalColor += sunColor * wideGlow;
    }
    
    // Apply the tint, the exposure and the vertex color.
    finalColor *= tintColor;
    finalColor *= exposure;
    finalColor *= fColor.rgb;
    
    // Tone map the HDR color into [0, 1], then convert it to display space.
    finalColor = tonemapAces(finalColor);
    finalColor = linearToGamma(finalColor);
    
    fFragColor = vec4(finalColor, uColor.a * fColor.a);
}