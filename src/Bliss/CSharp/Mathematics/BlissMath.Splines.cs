using System.Numerics;

namespace Bliss.CSharp.Mathematics;

public static partial class BlissMath {
    
    /// <summary>
    /// Generate spline points from control points.
    /// </summary>
    /// <param name="controlPoints">The input control points array.</param>
    /// <param name="resolution">The resolution of the generated spline point array.</param>
    /// <returns>A <see cref="Vector3"/> array of generated spline points.</returns>
    // </summary>
    private static Vector3[] GenerateSplinePoints(Vector3[] controlPoints, int resolution) {
        if (controlPoints.Length < 3)
            return controlPoints;

        List<Vector3> splinePoints = new List<Vector3>();

        // Use Catmull-Rom spline interpolation
        for (int i = 0; i < controlPoints.Length - 1; i++) {
            Vector3 p0 = i > 0 ? controlPoints[i - 1] : controlPoints[i];
            Vector3 p1 = controlPoints[i];
            Vector3 p2 = controlPoints[i + 1];
            Vector3 p3 = i < controlPoints.Length - 2 ? controlPoints[i + 2] : controlPoints[i + 1];

            // Generate interpolated points between p1 and p2
            for (int j = 0; j < resolution; j++) {
                float t = (float) j / resolution;
                Vector3 interpolatedPoint = CatmullRomInterpolate(p0, p1, p2, p3, t);
                splinePoints.Add(interpolatedPoint);
            }
        }

        // Add the final point
        splinePoints.Add(controlPoints[controlPoints.Length - 1]);

        return splinePoints.ToArray();
    }

    /// <summary>
    /// Catmull-Rom Interpolation between four control points.
    /// </summary>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    private static Vector3 CatmullRomInterpolate(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
        float t2 = t * t;
        float t3 = t2 * t;

        Vector3 result = 0.5f * (
            (2.0f * p1) +
            (-p0 + p2) * t +
            (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t2 +
            (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3
        );

        return result;
    }
    
    /// <summary>
    /// Calculate the total spline length.
    /// </summary>
    /// <param name="splinePoints"></param>
    /// <returns></returns>
    private static float CalculateTotalSplineLength(Vector3[] splinePoints) {
        float totalLength = 0f;
        for (int i = 1; i < splinePoints.Length; i++) {
            totalLength += Vector3.Distance(splinePoints[i - 1], splinePoints[i]);
        }
        return totalLength;
    }
    
    /// <summary>
    /// Calculate the cumulative distances for each point in the spline.
    /// </summary>
    /// <param name="splinePoints"></param>
    /// <returns></returns>
    private static float[] CalculateCumulativeDistances(Vector3[] splinePoints) {
        float[] distances = new float[splinePoints.Length];
        distances[0] = 0f;

        for (int i = 1; i < splinePoints.Length; i++) {
            float segmentLength = Vector3.Distance(splinePoints[i - 1], splinePoints[i]);
            distances[i] = distances[i - 1] + segmentLength;
        }

        return distances;
    }

    public static Vector3[] GetSplinePoints(Vector3[] points, int resolution, out float totalSplineLength, out int adjustedStipples, out float[] cumulativeDistances, bool stipple = false) {

        var splinePoints = BlissMath.GenerateSplinePoints(points, resolution);

        // Calculate total spline length and distances
        
        totalSplineLength = CalculateTotalSplineLength(splinePoints); 
        cumulativeDistances = CalculateCumulativeDistances(splinePoints);

        // Calculate stipple data based on total spline length (using same logic as GetLineData)
        float stippleWorldSize = 0.2f;
        float exactStipples = totalSplineLength / stippleWorldSize; 
        
        adjustedStipples = (int) Math.Floor(exactStipples);
        if (adjustedStipples % 2 == 0) adjustedStipples += 1;

        Vector4 baseLineData = new Vector4(totalSplineLength, adjustedStipples, totalSplineLength, stipple ? 1.0F : 0.0F);
        return splinePoints;

    }

}