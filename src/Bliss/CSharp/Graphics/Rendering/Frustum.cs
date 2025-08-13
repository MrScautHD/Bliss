using System.Numerics;
using Bliss.CSharp.Geometry;

namespace Bliss.CSharp.Graphics.Rendering;

public class Frustum {
    
    /// <summary>
    /// An array of planes used in various calculations or spatial operations.
    /// </summary>
    private Plane[] _planes;
    
    /// <summary>
    /// Initializes a new instance of the Frustum class.
    /// </summary>
    public Frustum() {
        this._planes = new Plane[6];
    }

    /// <summary>
    /// Extracts frustum planes from the view-projection matrix.
    /// </summary>
    public void Extract(Matrix4x4 viewProjection) {
        
        // LEFT
        this._planes[0] = Plane.Normalize(new Plane(
            viewProjection.M14 + viewProjection.M11,
            viewProjection.M24 + viewProjection.M21,
            viewProjection.M34 + viewProjection.M31,
            viewProjection.M44 + viewProjection.M41
        ));

        // RIGHT
        this._planes[1] = Plane.Normalize(new Plane(
            viewProjection.M14 - viewProjection.M11,
            viewProjection.M24 - viewProjection.M21,
            viewProjection.M34 - viewProjection.M31,
            viewProjection.M44 - viewProjection.M41
        ));

        // BOTTOM
        this._planes[2] = Plane.Normalize(new Plane(
            viewProjection.M14 + viewProjection.M12,
            viewProjection.M24 + viewProjection.M22,
            viewProjection.M34 + viewProjection.M32,
            viewProjection.M44 + viewProjection.M42
        ));

        // TOP
        this._planes[3] = Plane.Normalize(new Plane(
            viewProjection.M14 - viewProjection.M12,
            viewProjection.M24 - viewProjection.M22,
            viewProjection.M34 - viewProjection.M32,
            viewProjection.M44 - viewProjection.M42
        ));

        // NEAR
        this._planes[4] = Plane.Normalize(new Plane(
            viewProjection.M13,
            viewProjection.M23,
            viewProjection.M33,
            viewProjection.M43
        ));
        
        // FAR
        this._planes[5] = Plane.Normalize(new Plane(
            viewProjection.M14 - viewProjection.M13,
            viewProjection.M24 - viewProjection.M23,
            viewProjection.M34 - viewProjection.M33,
            viewProjection.M44 - viewProjection.M43
        ));
    }
    
    /// <summary>
    /// Checks if a point is contained within the frustum.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <returns>True if the point is contained within the frustum, otherwise false.</returns>
    public bool ContainsPoint(Vector3 point) {
        foreach (var plane in this._planes) {
            float distance = Plane.DotCoordinate(plane, point);
            
            if (distance < 0) {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Checks if a sphere is contained within the frustum.
    /// </summary>
    /// <param name="center">The center of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <returns>True if the sphere is contained within the frustum, otherwise false.</returns>
    public bool ContainsSphere(Vector3 center, float radius) {
        foreach (var plane in this._planes) {
            float distance = Plane.DotCoordinate(plane, center);
            
            if (distance < -radius) {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Checks if a bounding box is contained within the frustum.
    /// </summary>
    /// <param name="box">The bounding box to check.</param>
    /// <returns>True if the bounding box is contained within the frustum, otherwise false.</returns>
    public bool ContainsBox(BoundingBox box) {
        foreach (var plane in this._planes) {
            bool allOutside = true;
            
            for (int i = 0; i < 8; i++) {
                float x = (i & 1) == 0 ? box.Min.X : box.Max.X;
                float y = (i & 2) == 0 ? box.Min.Y : box.Max.Y;
                float z = (i & 4) == 0 ? box.Min.Z : box.Max.Z;
                
                Vector3 corner = new Vector3(x, y, z);
                float distance = Plane.DotCoordinate(plane, corner);
                
                if (distance >= 0) {
                    allOutside = false;
                    break;
                }
            }
            
            if (allOutside) {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if a oriented box is contained within the frustum.
    /// </summary>
    /// <param name="box">The oriented box to check.</param>
    /// <param name="origin">The origin of the oriented box.</param>
    /// <param name="rotation">The rotation of the oriented box.</param>
    /// <returns>True if the oriented box is contained within the frustum, otherwise false.</returns>
    public bool ContainsOrientedBox(BoundingBox box, Vector3 origin, Quaternion rotation) {
        foreach (var plane in this._planes) {
            bool allOutside = true;

            for (int i = 0; i < 8; i++) {
                float x = (i & 1) == 0 ? box.Min.X : box.Max.X;
                float y = (i & 2) == 0 ? box.Min.Y : box.Max.Y;
                float z = (i & 4) == 0 ? box.Min.Z : box.Max.Z;

                Vector3 corner = Vector3.Transform(new Vector3(x, y, z) - origin, rotation) + origin;
                float distance = Plane.DotCoordinate(plane, corner);
            
                if (distance >= 0) {
                    allOutside = false;
                    break;
                }
            }

            if (allOutside) {
                return false;
            }
        }

        return true;
    }
    /* https://en.wikipedia.org/wiki/Separating_axis_theorem 
       This could probably be optimised */
    public bool IntersectsFrustum(Frustum other) {
        var thisCorners = GetCorners();
        var otherCorners = other.GetCorners();

        // Check this frustum's planes against other frustum's corners
        foreach (var plane in _planes) {
            bool allOutside = true;
            foreach (var corner in otherCorners) {
                if (Plane.DotCoordinate(plane, corner) >= 0) {
                    allOutside = false;
                    break;
                }
            }

            if (allOutside) return false;
        }

        // Check other frustum's planes against this frustum's corners
        foreach (var plane in other._planes) {
            bool allOutside = true;
            foreach (var corner in thisCorners) {
                if (Plane.DotCoordinate(plane, corner) >= 0) {
                    allOutside = false;
                    break;
                }
            }

            if (allOutside) return false;
        }

        return true;
    }
    public Vector3[] GetCorners() {
        // L: 0, R: 1, B: 2, T: 3, N: 4, F: 5
        return
        [
            IntersectPlanes(_planes[0], _planes[2], _planes[4]),
            IntersectPlanes(_planes[0], _planes[3], _planes[4]),
            IntersectPlanes(_planes[1], _planes[3], _planes[4]),
            IntersectPlanes(_planes[1], _planes[2], _planes[4]),
            IntersectPlanes(_planes[0], _planes[2], _planes[5]),
            IntersectPlanes(_planes[0], _planes[3], _planes[5]),
            IntersectPlanes(_planes[1], _planes[3], _planes[5]),
            IntersectPlanes(_planes[1], _planes[2], _planes[5])
        ];
    }
    public Vector3 IntersectPlanes(Plane p1, Plane p2, Plane p3) {
        // Solving intersection point of 3 planes using Cramer's Rule
        var n1 = new Vector3(p1.Normal.X, p1.Normal.Y, p1.Normal.Z);
        var n2 = new Vector3(p2.Normal.X, p2.Normal.Y, p2.Normal.Z);
        var n3 = new Vector3(p3.Normal.X, p3.Normal.Y, p3.Normal.Z);

        var denom = Vector3.Dot(n1, Vector3.Cross(n2, n3));

        if (MathF.Abs(denom) < 1e-6f) return Vector3.Zero; // Parallel planes

        var c1 = (-p1.D * Vector3.Cross(n2, n3));
        var c2 = (-p2.D * Vector3.Cross(n3, n1));
        var c3 = (-p3.D * Vector3.Cross(n1, n2));

        return (c1 + c2 + c3) / denom;
    }
}