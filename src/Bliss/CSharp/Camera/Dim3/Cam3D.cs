using System.Numerics;
using Bliss.CSharp.Graphics.Rendering;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Mathematics;
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
    /// Defines the current operational mode of the camera.
    /// This determines the behavior and control mechanics of the camera,
    /// such as whether it's in Free mode, Orbital mode, etc.
    /// </summary>
    public CameraMode Mode;

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
    /// target, orientation, projection type, camera mode, field of view, and clipping planes.
    /// </summary>
    /// <param name="width">The width of the viewport or rendering area.</param>
    /// <param name="height">The height of the viewport or rendering area.</param>
    /// <param name="position">The initial position of the camera in 3D space.</param>
    /// <param name="target">The point in space that the camera is looking at.</param>
    /// <param name="up">The up vector for the camera orientation; defaults to <see cref="Vector3.UnitY"/> if not specified.</param>
    /// <param name="projectionType">The type of projection to use (e.g., perspective or orthographic); defaults to <see cref="ProjectionType.Perspective"/>.</param>
    /// <param name="mode">The camera mode, such as <see cref="CameraMode.Free"/> or other modes; defaults to <see cref="CameraMode.Free"/>.</param>
    /// <param name="fov">The field of view angle in degrees; defaults to 70.0 degrees.</param>
    /// <param name="nearPlane">The distance to the near clipping plane; defaults to 0.001 units.</param>
    /// <param name="farPlane">The distance to the far clipping plane; defaults to 1000.0 units.</param>
    public Cam3D(int width, int height, Vector3 position, Vector3 target, Vector3? up = default, ProjectionType projectionType = ProjectionType.Perspective, CameraMode mode = CameraMode.Free, float fov = 70.0F, float nearPlane = 0.001F, float farPlane = 1000.0F) {
        this.Position = position;
        this.Target = target;
        this.Up = up ?? Vector3.UnitY;
        this.ProjectionType = projectionType;
        this.Mode = mode;
        this.Fov = fov;
        this.NearPlane = nearPlane;
        this.FarPlane = farPlane;
        this._frustum = new Frustum();
        this.Resize(width, height);
    }

    public void Update(double delta) {
        Logger.Error(this.GetRotation() + "");
        
        switch (this.Mode) {
            case CameraMode.Free:
                if (!Input.IsGamepadAvailable(0)) {
                    this.SetYaw(this.GetYaw() - (Input.GetMouseDelta().X * 10) * (float) delta, true);
                    this.SetPitch(this.GetPitch() - (Input.GetMouseDelta().Y * 10) * (float) delta, true);

                    //if (Input.IsKeyDown(KeyboardKey.W)) {
                    //    this.MoveForward(this.MovementSpeed * Time.GetFrameTime(), true);
                    //}
                    //
                    //if (Input.IsKeyDown(KeyboardKey.S)) {
                    //    this.MoveForward(-this.MovementSpeed * Time.GetFrameTime(), true);
                    //}
                    //
                    //if (Input.IsKeyDown(KeyboardKey.A)) {
                    //    this.MoveRight(-this.MovementSpeed * Time.GetFrameTime(), true);
                    //}
                    //
                    //if (Input.IsKeyDown(KeyboardKey.D)) {
                    //    this.MoveRight(this.MovementSpeed * Time.GetFrameTime(), true);
                    //}
//
                    //if (Input.IsKeyDown(KeyboardKey.Space)) {
                    //    this.MoveUp(this.MovementSpeed * Time.GetFrameTime());
                    //}
                    //
                    //if (Input.IsKeyDown(KeyboardKey.LeftShift)) {
                    //    this.MoveUp(-this.MovementSpeed * Time.GetFrameTime());
                    //}
                }
                else {
                    
                }
                break;
        }
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
    public Matrix4x4 GetProjection() { // TODO: Create it not everytime it get called just one time!
        if (this.ProjectionType == ProjectionType.Perspective) {
            return Matrix4x4.CreatePerspectiveFieldOfView(float.DegreesToRadians(this.Fov), this.AspectRatio, this.NearPlane, this.FarPlane);
        }

        float top = this.Fov / 2.0F;
        float right = top * this.AspectRatio;
        
        return Matrix4x4.CreateOrthographicOffCenter(-right, right, -top, top, this.NearPlane, this.FarPlane);
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
        Vector3 rotation = Quaternion.CreateFromRotationMatrix(this.GetView()).ToEuler();
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
        float finalAngle = float.DegreesToRadians(angle - this.GetYaw());
        
        // Calculate view vector.
        Vector3 view = this.Target - this.Position;

        // Calculate target pos.
        Vector3 targetPosition = BlissMath.Vector3RotateByAxisAngle(view, this.Up, finalAngle);
        
        if (rotateAroundTarget) {
            this.Position = this.Target - targetPosition;
        }
        else {
            this.Target = this.Position + targetPosition;
        }
    }
    
    /// <summary>
    /// Retrieves the current pitch angle of the camera.
    /// </summary>
    /// <returns>The current pitch angle in degrees.</returns>
    public float GetPitch() {
        return this.GetRotation().X;
    }

    /// <summary>
    /// Sets the pitch angle of the camera, rotating around the specified target if indicated.
    /// </summary>
    /// <param name="angle">The desired pitch angle in degrees.</param>
    /// <param name="rotateAroundTarget">If true, rotates the camera around the target point; otherwise rotates around its own position.</param>
    public void SetPitch(float angle, bool rotateAroundTarget) {
        float finalAngle = float.DegreesToRadians(angle - this.GetPitch());
        
        // Calculate view vector.
        Vector3 view = this.Target - this.Position;

        // Calculate max allowable upward and downward angles.
        float maxAngleUp = BlissMath.Vector3Angle(this.Up, view) - 0.001F;
        float maxAngleDown = -BlissMath.Vector3Angle(Vector3.Negate(this.Up), view) + 0.001F;
        
        // Clamp the angle within the min and max bounds.
        finalAngle = Math.Clamp(finalAngle, maxAngleDown, maxAngleUp);
        
        // Calculate target pos.
        Vector3 targetPosition = BlissMath.Vector3RotateByAxisAngle(view, this.GetRight(), finalAngle);

        if (rotateAroundTarget) {
            this.Position = this.Target - targetPosition;
        }
        else {
            this.Target = this.Position + targetPosition;
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
        float finalAngle = float.DegreesToRadians(angle - this.GetRoll());
        this.Up = BlissMath.Vector3RotateByAxisAngle(this.Up, this.GetForward(), finalAngle);
    }

    /// <summary>
    /// Retrieves the camera frustum by extracting the frustum planes based on the current view and projection matrices.
    /// </summary>
    /// <returns>The updated frustum containing the extracted planes.</returns>
    public Frustum GetFrustum() {
        this._frustum.Extract(this.GetProjection() * this.GetView());
        return this._frustum;
    }
} 