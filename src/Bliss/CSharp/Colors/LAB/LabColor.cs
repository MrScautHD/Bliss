namespace Bliss.CSharp.Colors.LAB;

public readonly struct LabColor {
    public readonly float L; // Lightness
    public readonly float A; // Green-Red component
    public readonly float B; // Blue-Yellow component
    
    /// <summary>
    /// LAB representation of color
    /// </summary>
    private LabColor(float l, float a, float b) {
        this.L = l;
        this.A = a;
        this.B = b;
    }
    // Fast gamma correction functions
    private static float GammaToLinear(float value) {
        return value <= 0.04045f ? value / 12.92f : MathF.Pow((value + 0.055f) / 1.055f, 2.4f);
    }

    private static float LinearToGamma(float value) {
        return value <= 0.0031308f ? value * 12.92f : 1.055f * MathF.Pow(value, 1f / 2.4f) - 0.055f;
    }
    // LAB helper functions
    private static float LabF(float t) {
        const float delta = 6f / 29f;
        const float deltaSquared = delta * delta;
        const float deltaCubed = deltaSquared * delta;
        
        return t > deltaCubed ? MathF.Pow(t, 1f / 3f) : t / (3f * deltaSquared) + 4f / 29f;
    }
    private static float LabFInverse(float t) {
        const float delta = 6f / 29f;
        const float deltaSquared = delta * delta;
        
        return t > delta ? t * t * t : 3f * deltaSquared * (t - 4f / 29f);
    }
    private static (float L, float A, float B) RgbToLab(byte r, byte g, byte b) {
        // RGB to linear RGB
        float rLinear = GammaToLinear(r / 255f);
        float gLinear = GammaToLinear(g / 255f);
        float bLinear = GammaToLinear(b / 255f);

        // Linear RGB to XYZ (sRGB/D65)
        float x = rLinear * 0.4124564f + gLinear * 0.3575761f + bLinear * 0.1804375f;
        float y = rLinear * 0.2126729f + gLinear * 0.7151522f + bLinear * 0.0721750f;
        float z = rLinear * 0.0193339f + gLinear * 0.1191920f + bLinear * 0.9503041f;

        // Normalize for D65 illuminant
        x /= 0.95047f;
        y /= 1.00000f;
        z /= 1.08883f;

        // XYZ to LAB
        float fx = LabF(x);
        float fy = LabF(y);
        float fz = LabF(z);

        float L = 116f * fy - 16f;
        float A = 500f * (fx - fy);
        float B = 200f * (fy - fz);

        return (L, A, B);
    }
    private static (byte r, byte g, byte b) LabToRgb(float L, float A, float B) {
        // LAB to XYZ
        float fy = (L + 16f) / 116f;
        float fx = A / 500f + fy;
        float fz = fy - B / 200f;

        float x = LabFInverse(fx) * 0.95047f;
        float y = LabFInverse(fy) * 1.00000f;
        float z = LabFInverse(fz) * 1.08883f;

        // XYZ to linear RGB
        float rLinear = x *  3.2404542f + y * -1.5371385f + z * -0.4985314f;
        float gLinear = x * -0.9692660f + y *  1.8760108f + z *  0.0415560f;
        float bLinear = x *  0.0556434f + y * -0.2040259f + z *  1.0572252f;

        // Linear RGB to gamma-corrected RGB
        float r = LinearToGamma(rLinear);
        float g = LinearToGamma(gLinear);
        float b = LinearToGamma(bLinear);

        return ((byte)Math.Clamp(r * 255f + 0.5f, 0, 255), (byte)Math.Clamp(g * 255f + 0.5f, 0, 255), (byte)Math.Clamp(b * 255f + 0.5f, 0, 255)
        );
    }
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    private static Color InterpolateLab(Color color1, Color color2, float t) {
        t = Math.Clamp(t, 0f, 1f);
        
        var lab1 = RgbToLab(color1.R, color1.G, color1.B);
        var lab2 = RgbToLab(color2.R, color2.G, color2.B);
        
        var labResult = (
            L: Lerp(lab1.L, lab2.L, t),
            A: Lerp(lab1.A, lab2.A, t),
            B: Lerp(lab1.B, lab2.B, t)
        );
        
        var (r, g, b) = LabToRgb(labResult.L, labResult.A, labResult.B);
        return new Color(r, g, b, (byte)Lerp(color1.A, color2.A, t));
    }
    public static Color Interpolate(Color color1, Color color2, float t) {
        return InterpolateLab(color1, color2, t);
    }
}