using System.Numerics;
using Bliss.CSharp.Graphics.Rendering;
using Vortice.Mathematics;
using Viewport = Veldrid.Viewport;

namespace Bliss.CSharp.Camera.Dim3;

public class Cam3D : ICam {
    
    /// <summary>
    /// References the currently active instance of the Cam3D class.
    /// Used to determine which camera is currently rendering the scene.
    /// Can be accessed from other classes to retrieve camera-specific properties and methods.
    /// </summary>
    internal static Cam3D? ActiveCamera;

    /// <summary>
    /// Defines the portion of the render target that a camera will render to.
    /// Specifies the region of the screen or render target the camera will draw its contents to.
    /// </summary>
    public Viewport Viewport { get; private set; }

    /// <summary>
    /// Represents the ratio between the width and height of the camera's viewport.
    /// Used to adjust the projection matrix for rendering the 3D scene correctly on the screen.
    /// </summary>
    public float AspectRatio { get; private set; }

    /// <summary>
    /// Represents the position of the camera in a 3D space.
    /// This defines the location from which the camera is capturing its view.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Represents the focal point that the camera is aimed at in the 3D space.
    /// Determines the direction in which the camera is looking.
    /// </summary>
    public Vector3 Target;

    /// <summary>
    /// Represents the upward direction vector for the camera, determining its orientation in the 3D space
    /// relative to its position and target.
    /// </summary>
    public Vector3 Up;

    /// <summary>
    /// Represents the type of projection used by the camera, either Perspective or Orthographic,
    /// influencing how 3D scenes are rendered onto a 2D viewport.
    /// </summary>
    public ProjectionType ProjectionType;

    /// <summary>
    /// Defines the field of view (FOV) angle for the camera, determining
    /// the extent of the observable world that is seen at any given moment.
    /// </summary>
    public float Fov;

    /// <summary>
    /// Specifies the near clipping plane distance for the camera, determining
    /// the minimum depth at which objects are rendered in the 3D scene.
    /// </summary>
    public float NearPlane;

    /// <summary>
    /// Specifies the far clipping plane distance for the camera, determining
    /// the maximum depth at which objects are rendered in the 3D scene.
    /// </summary>
    public float FarPlane;

    /// <summary>
    /// Represents the viewing frustum for the camera, which determines the
    /// visible field and boundary for objects in the 3D scene.
    /// </summary>
    private Frustum _frustum;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Cam3D"/> class, configuring the camera's position,
    /// target, orientation, projection type, field of view, and clipping planes.
    /// </summary>
    /// <param name="width">The width of the viewport or rendering area.</param>
    /// <param name="height">The height of the viewport or rendering area.</param>
    /// <param name="position">The initial position of the camera in 3D space.</param>
    /// <param name="target">The point in space that the camera is looking at.</param>
    /// <param name="up">The up vector for the camera orientation; defaults to <see cref="Vector3.UnitY"/> if not specified.</param>
    /// <param name="projectionType">The type of projection to use (e.g., perspective or orthographic); defaults to <see cref="ProjectionType.Perspective"/>.</param>
    /// <param name="fov">The field of view angle in degrees; defaults to 70.0 degrees.</param>
    /// <param name="nearPlane">The distance to the near clipping plane; defaults to 0.001 units.</param>
    /// <param name="farPlane">The distance to the far clipping plane; defaults to 1000.0 units.</param>
    public Cam3D(int width, int height, Vector3 position, Vector3 target, Vector3? up = default, ProjectionType projectionType = ProjectionType.Perspective, float fov = 70.0F, float nearPlane = 0.001F, float farPlane = 1000.0F) {
        this.Position = position;
        this.Target = target;
        this.Up = up ?? Vector3.UnitY;
        this.ProjectionType = projectionType;
        this.Fov = fov;
        this.NearPlane = nearPlane;
        this.FarPlane = farPlane;
        this._frustum = new Frustum();
        this.Resize(width, height);
    }

    /// <summary>
    /// Resizes the viewport and updates the aspect ratio based on the given width and height.
    /// </summary>
    /// <param name="width">The new width of the viewport.</param>
    /// <param name="height">The new height of the viewport.</param>
    public void Resize(int width, int height) {
        this.Viewport = new Viewport(0, 0, width, height, 0, 0);
        this.AspectRatio = (float) width / (float) height;
    }
    
    /// <summary>
    /// Generates the projection matrix based on the current camera settings, such as projection type, field of view (FOV), aspect ratio, near plane, and far plane distances.
    /// </summary>
    /// <returns>The calculated projection matrix.</returns>
    public Matrix4x4 GetProjection() {
        if (this.ProjectionType == ProjectionType.Perspective) {
            return Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.ToRadians(this.Fov), this.AspectRatio, this.NearPlane, this.FarPlane);
        }
        
        return Matrix4x4.CreateOrthographicOffCenter(-40, 40, 40, -40, this.NearPlane, this.FarPlane);
    }

    /// <summary>
    /// Constructs and returns the view matrix for the camera based on its position, target, and up vector.
    /// </summary>
    /// <returns>The view matrix of the camera.</returns>
    public Matrix4x4 GetView() {
        return Matrix4x4.CreateLookAt(this.Position, this.Target, this.Up);
    }
    
    /// <summary>
    /// Sets the current instance of Cam3D as the active camera for rendering 3D scenes.
    /// </summary>
    public void Begin() {
        ActiveCamera = this;
    }

    /// <summary>
    /// Ends the current 3D rendering session and deactivates the camera.
    /// </summary>
    public void End() {
        ActiveCamera = null;
    }

    /// <summary>
    /// Computes and returns the forward direction vector based on the camera's position and target.
    /// </summary>
    /// <returns>A Vector3 representing the forward direction of the camera.</returns>
    public Vector3 GetForward() {
        return Vector3.Normalize(this.Target - this.Position);
    }

    /// <summary>
    /// Computes and returns the right direction vector based on the camera's forward direction and up vector.
    /// </summary>
    /// <returns>A Vector3 representing the right direction of the camera.</returns>
    public Vector3 GetRight() {
        return Vector3.Cross(this.GetForward(), this.Up);
    }

    /// <summary>
    /// Retrieves the current rotation vector of the camera.
    /// </summary>
    /// <returns>A Vector3 representing the current rotation of the camera.</returns>
    public Vector3 GetRotation() {
        Matrix4x4 lookAt = Matrix4x4.CreateLookAt(this.Position, this.Target, this.Up);
        Vector3 rotation = Quaternion.CreateFromRotationMatrix(lookAt).ToEuler();
        return new Vector3(float.RadiansToDegrees(rotation.X), float.RadiansToDegrees(rotation.Y), float.RadiansToDegrees(rotation.Z));
    }

    /// <summary>
    /// Retrieves the current yaw (rotation around the Y-axis) of the camera.
    /// </summary>
    /// <returns>The yaw value in degrees.</returns>
    public float GetYaw() {
        return this.GetRotation().Y;
    }

    /// <summary>
    /// Sets the yaw of the camera by rotating it around the target or adjusting the target position, based on the specified angle.
    /// </summary>
    /// <param name="angle">The angle in degrees to rotate the camera.</param>
    /// <param name="rotateAroundTarget">A boolean indicating whether to rotate the camera around the target or adjust the target position.</param>
    public void SetYaw(float angle, bool rotateAroundTarget) {
        float difference = angle - this.GetYaw();
        Matrix4x4 rotationMatrix = Matrix4x4.CreateFromAxisAngle(this.Up, float.DegreesToRadians(difference));
        
        if (rotateAroundTarget) {
            Vector3 direction = Vector3.Transform(this.Position - this.Target, rotationMatrix);
            this.Position = this.Target + direction;
        }
        else {
            Vector3 direction = Vector3.Transform(this.Target - this.Position, rotationMatrix);
            this.Target = this.Position + direction;
        }
    }
    
    /// <summary>
    /// Retrieves the current pitch angle of the camera.
    /// </summary>
    /// <returns>The current pitch angle in degrees.</returns>
    public float GetPitch() {
        return this.GetRotation().X;
    }
    
    public void SetPitch(float angle, bool rotateAroundTarget) { // TODO: This is fully broken i need to check it after dinner.
        float difference = angle - this.GetPitch();
        Vector3 right = this.GetRight();
        Matrix4x4 rotationMatrix = Matrix4x4.CreateFromAxisAngle(right, float.DegreesToRadians(difference));

        if (rotateAroundTarget) {
            Vector3 direction = Vector3.Transform(this.Position - this.Target, rotationMatrix);
            this.Position = this.Target + direction;
        }
        else {
            Vector3 direction = Vector3.Transform(this.Target - this.Position, rotationMatrix);
            this.Target = this.Position + direction;
        }
    }
    
    /// <summary>
    /// Retrieves the roll component of the camera's rotation.
    /// </summary>
    /// <returns>The roll angle in degrees.</returns>
    public float GetRoll() {
        return this.GetRotation().Z;
    }

    /// <summary>
    /// Sets the roll angle of the camera by rotating around the forward axis.
    /// </summary>
    /// <param name="angle">The angle in degrees by which to set the roll.</param>
    public void SetRoll(float angle) {
        float difference = angle - this.GetRoll();
        Matrix4x4 rotationMatrix = Matrix4x4.CreateFromAxisAngle(this.GetForward(), float.DegreesToRadians(difference));
        
        this.Up = Vector3.Transform(this.Up, rotationMatrix);
        this.Target = Vector3.Transform(this.Target - this.Position, rotationMatrix) + this.Position;
    }

    /// <summary>
    /// Retrieves the camera frustum by extracting the frustum planes based on the current view and projection matrices.
    /// </summary>
    /// <returns>The updated frustum containing the extracted planes.</returns>
    public Frustum GetFrustum() {
        this._frustum.Extract(this.GetView() * this.GetProjection());
        return this._frustum;
    }
} 