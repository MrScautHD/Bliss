using System.Numerics;
using Bliss.CSharp.Graphics.Rendering;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Gamepads;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Mathematics;
using Bliss.CSharp.Transformations;
using Veldrid;
using Vortice.Mathematics;

namespace Bliss.CSharp.Camera.Dim3;

public class Cam3D : ICam {
    
    /// <summary>
    /// References the currently active instance of the Cam3D class.
    /// Used to determine which camera is currently rendering the scene.
    /// Can be accessed from other classes to retrieve camera-specific properties and methods.
    /// </summary>
    public static Cam3D? ActiveCamera { get; private set; }

    /// <summary>
    /// Defines the portion of the render target that a camera will render to.
    /// Specifies the region of the screen or render target the camera will draw its contents to.
    /// </summary>
    public Rectangle Size { get; private set; }

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
    /// Determines the sensitivity of the camera to mouse movements.
    /// This affects how fast the camera rotates or moves in response to mouse inputs.
    /// Higher values result in faster and more responsive camera movements.
    /// </summary>
    public float MouseSensitivity;

    /// <summary>
    /// Specifies the speed at which the camera can move within the scene.
    /// Adjust this value to control how fast the camera translates in the 3D space.
    /// Typically used in conjunction with user input to navigate the scene.
    /// </summary>
    public float MovementSpeed;

    /// <summary>
    /// Represents the speed at which the camera orbits around a target.
    /// Determines how quickly the camera moves when controlled to orbit in any direction.
    /// Typically used in orbital or tracking camera modes.
    /// </summary>
    public float OrbitalSpeed;

    /// <summary>
    /// Represents the viewing frustum for the camera, which determines the
    /// visible field and boundary for objects in the 3D scene.
    /// </summary>
    private Frustum _frustum;

    /// <summary>
    /// Stores the projection matrix used for transforming 3D coordinates to 2D screen coordinates.
    /// Updated based on the camera's projection type, field of view, aspect ratio, and near/far planes.
    /// Accessed to retrieve the current projection matrix for rendering.
    /// </summary>
    private Matrix4x4 _projection;

    /// <summary>
    /// Stores the view matrix of the camera.
    /// The view matrix represents the transformation from world coordinates to camera coordinates.
    /// It is used in rendering to position and orient the camera in the scene.
    /// </summary>
    private Matrix4x4 _view;

    /// <summary>
    /// Stores the current CommandList being used by the Cam3D instance.
    /// Allows for the execution of rendering commands within the scope of
    /// the camera's render operations.
    /// </summary>
    private CommandList _currentCommandList;
    
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
    public Cam3D(uint width, uint height, Vector3 position, Vector3 target, Vector3? up = null, ProjectionType projectionType = ProjectionType.Perspective, CameraMode mode = CameraMode.Free, float fov = 70.0F, float nearPlane = 0.1F, float farPlane = 1000.0F) {
        this.Position = position;
        this.Target = target;
        this.Up = up ?? Vector3.UnitY;
        this.ProjectionType = projectionType;
        this.Mode = mode;
        this.Fov = fov;
        this.NearPlane = nearPlane;
        this.FarPlane = farPlane;
        this.MouseSensitivity = 10.0F;
        this.MovementSpeed = 10.0F;
        this.OrbitalSpeed = 0.5F;
        this._frustum = new Frustum();
        this.Resize(width, height);
    }

    public void Update(double timeStep) {
        switch (this.Mode) {
            case CameraMode.Free:
                if (!Input.IsGamepadAvailable(0)) {
                    this.SetYaw(this.GetYaw() - (Input.GetMouseDelta().X * this.MouseSensitivity) * (float) timeStep, false);
                    this.SetPitch(this.GetPitch() - (Input.GetMouseDelta().Y * this.MouseSensitivity) * (float) timeStep, false);

                    if (Input.IsKeyDown(KeyboardKey.W)) {
                        this.MoveForward(this.MovementSpeed * (float) timeStep, true);
                    }
                    
                    if (Input.IsKeyDown(KeyboardKey.S)) {
                        this.MoveForward(-this.MovementSpeed * (float) timeStep, true);
                    }
                    
                    if (Input.IsKeyDown(KeyboardKey.A)) {
                        this.MoveRight(-this.MovementSpeed * (float) timeStep, true);
                    }
                    if (Input.IsKeyDown(KeyboardKey.D)) {
                        this.MoveRight(this.MovementSpeed * (float) timeStep, true);
                    }
                    
                    if (Input.IsKeyDown(KeyboardKey.Space)) {
                        this.MoveUp(this.MovementSpeed * (float) timeStep);
                    }
                    
                    if (Input.IsKeyDown(KeyboardKey.ShiftLeft)) {
                        this.MoveUp(-this.MovementSpeed * (float) timeStep);
                    }
                }
                else {
                    this.SetYaw(this.GetYaw() - (Input.GetGamepadAxisMovement(0, GamepadAxis.RightX) * 6) * this.MouseSensitivity * (float) timeStep, false);
                    this.SetPitch(this.GetPitch() - (Input.GetGamepadAxisMovement(0, GamepadAxis.RightY) * 6) * this.MouseSensitivity * (float) timeStep, false);
                    
                    this.MoveForward(this.MovementSpeed * Input.GetGamepadAxisMovement(0, GamepadAxis.TriggerRight) * (float) timeStep, false);
                    this.MoveForward(-this.MovementSpeed * Input.GetGamepadAxisMovement(0, GamepadAxis.TriggerLeft) * (float) timeStep, false);
                    
                    if (Input.IsGamepadButtonDown(0, GamepadButton.RightShoulder)) {
                        this.MoveRight(this.MovementSpeed * (float) timeStep, true);
                    }
                    
                    if (Input.IsGamepadButtonDown(0, GamepadButton.LeftShoulder)) {
                        this.MoveRight(-this.MovementSpeed * (float) timeStep, true);
                    }
                    
                    if (Input.IsGamepadButtonDown(0, GamepadButton.RightStick)) {
                        this.MoveUp(this.MovementSpeed * (float) timeStep);
                    }
                    
                    if (Input.IsGamepadButtonDown(0, GamepadButton.LeftStick)) {
                        this.MoveUp(-this.MovementSpeed * (float) timeStep);
                    }
                }
                break;
            
            case CameraMode.Orbital:
                Matrix4x4 rotation = Matrix4x4.CreateFromAxisAngle(this.Up, this.OrbitalSpeed * (float) timeStep);
                Vector3 view = this.Position - this.Target;
                Vector3 transform = Vector3.Transform(view, rotation);
                this.Position = this.Target + transform;

                if (Input.IsMouseScrolling(out Vector2 wheelDelta)) {
                    this.MoveToTarget(-wheelDelta.Y);
                }
                break;
            
            case CameraMode.FirstPerson:
                if (!Input.IsGamepadAvailable(0)) {
                    this.SetYaw(this.GetYaw() - (Input.GetMouseDelta().X * this.MouseSensitivity) * (float) timeStep, false);
                    this.SetPitch(this.GetPitch() - (Input.GetMouseDelta().Y * this.MouseSensitivity) * (float) timeStep, false);
                }
                else {
                    this.SetYaw(this.GetYaw() - (Input.GetGamepadAxisMovement(0, GamepadAxis.RightX) * 6) * this.MouseSensitivity * (float) timeStep, false);
                    this.SetPitch(this.GetPitch() - (Input.GetGamepadAxisMovement(0, GamepadAxis.RightY) * 6) * this.MouseSensitivity * (float) timeStep, false);
                }
                break;
            
            case CameraMode.ThirdPerson:
                if (!Input.IsGamepadAvailable(0)) {
                    this.SetYaw(this.GetYaw() - (Input.GetMouseDelta().X * this.MouseSensitivity) * (float) timeStep, true);
                    this.SetPitch(this.GetPitch() - (Input.GetMouseDelta().Y * this.MouseSensitivity) * (float) timeStep, true);
                }
                else {
                    this.SetYaw(this.GetYaw() + (Input.GetGamepadAxisMovement(0, GamepadAxis.RightX) * 6) * this.MouseSensitivity * (float) timeStep, true);
                    this.SetPitch(this.GetPitch() + (Input.GetGamepadAxisMovement(0, GamepadAxis.RightY) * 6) * this.MouseSensitivity * (float) timeStep, true);
                }
                break;
        }
    }

    /// <summary>
    /// Resizes the viewport and updates the aspect ratio based on the given width and height.
    /// </summary>
    /// <param name="width">The new width of the viewport.</param>
    /// <param name="height">The new height of the viewport.</param>
    public void Resize(uint width, uint height) {
        this.Size = new Rectangle(0, 0, (int) width, (int) height);
        this.AspectRatio = (float) width / (float) height;
    }

    /// <summary>
    /// Sets the specified command list as the current command list for rendering operations and updates
    /// the camera's projection and view matrices. Also clears the depth stencil and sets this camera as the active camera.
    /// </summary>
    /// <param name="commandList">The command list to set as the current command list for rendering operations.</param>
    public void Begin(CommandList commandList) {
        this._currentCommandList = commandList;
        this.UpdateProjection();
        this.UpdateView();
        
        commandList.ClearDepthStencil(1.0F);
        ActiveCamera = this;
    }

    /// <summary>
    /// Ends the current 3D rendering session and deactivates the camera.
    /// </summary>
    public void End() {
        this._currentCommandList.ClearDepthStencil(0.0F);
        ActiveCamera = null;
    }
    
    /// <summary>
    /// Generates the projection matrix based on the current camera settings, such as projection type, field of view (FOV), aspect ratio, near plane, and far plane distances.
    /// </summary>
    /// <returns>The calculated projection matrix.</returns>
    public Matrix4x4 GetProjection() {
        return this._projection;
    }

    /// <summary>
    /// Constructs and returns the view matrix for the camera based on its position, target, and up vector.
    /// </summary>
    /// <returns>The view matrix of the camera.</returns>
    public Matrix4x4 GetView() {
        return this._view;
    }
    
    /// <summary>
    /// Computes and returns the forward direction vector based on the camera's position and target.
    /// </summary>
    /// <returns>A Vector3 representing the forward direction of the camera.</returns>
    public Vector3 GetForward() {
        return Vector3.Normalize(this.Target - this.Position);
    }

    /// <summary>
    /// Moves the camera forward by a specified distance.
    /// </summary>
    /// <param name="distance">The distance by which to move the camera forward.</param>
    /// <param name="moveInWorldPlane">Determines whether to constrain movement to the world plane, ignoring the Y component.</param>
    public void MoveForward(float distance, bool moveInWorldPlane) {
        Vector3 forward = this.GetForward();
        
        if (moveInWorldPlane) {
            forward.Y = 0;
            forward = Vector3.Normalize(forward);
        }

        forward *= distance;

        this.Position += forward;
        this.Target += forward;
    }

    /// <summary>
    /// Computes and returns the right direction vector based on the camera's forward direction and up vector.
    /// </summary>
    /// <returns>A Vector3 representing the right direction of the camera.</returns>
    public Vector3 GetRight() {
        return Vector3.Cross(this.GetForward(), this.Up);
    }

    /// <summary>
    /// Moves the camera to the right by a specified distance, with an option to constrain the movement to the world plane.
    /// </summary>
    /// <param name="distance">The distance to move the camera to the right.</param>
    /// <param name="moveInWorldPlane">If set to <c>true</c>, the camera will move parallel to the ground plane and will not change its Y position.</param>
    public void MoveRight(float distance, bool moveInWorldPlane) {
        Vector3 right = this.GetRight();
        
        if (moveInWorldPlane) {
            right.Y = 0;
            right = Vector3.Normalize(right);
        }

        right *= distance;

        this.Position += right;
        this.Target += right;
    }

    /// <summary>
    /// Moves the camera upward by a specified distance.
    /// </summary>
    /// <param name="distance">The distance to move the camera upward.</param>
    public void MoveUp(float distance) {
        Vector3 up = this.Up * distance;
        
        this.Position += up;
        this.Target += up;
    }

    /// <summary>
    /// Moves the camera towards or away from its target by modifying the distance between the camera's position and its target point.
    /// </summary>
    /// <param name="delta">The amount by which to adjust the distance to the target. Positive values move the camera closer to the target, and negative values move it further away.</param>
    public void MoveToTarget(float delta) {
        float distance = Vector3.Distance(this.Position, this.Target) + delta;

        if (distance <= 0) {
            distance = 0.001F;
        }

        this.Position = this.Target + this.GetForward() * -distance;
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
        this._frustum.Extract(this.GetView() * this.GetProjection());
        return this._frustum;
    }
    
    /// <summary>
    /// Converts a given screen position to world coordinates using the camera projection and view matrices.
    /// </summary>
    /// <param name="position">The screen position to transform.</param>
    /// <returns>A <see cref="Vector3"/> representing the corresponding world coordinates.</returns>
    public Vector3 GetScreenToWorld(Vector2 position) {
        Matrix4x4.Invert(this.GetView() * this.GetProjection(), out Matrix4x4 result);
        Vector4 screenPosition = new Vector4(position, 1.0F, 1.0F);
        Vector4 worldPosition = Vector4.Transform(screenPosition, result);
        
        if (worldPosition.W != 0.0F) {
            worldPosition /= worldPosition.W;
        }
    
        return new Vector3(worldPosition.X, worldPosition.Y, worldPosition.Z);
    }
    
    /// <summary>
    /// Converts a given world position to screen coordinates based on the camera's current view and projection matrices.
    /// </summary>
    /// <param name="position">The world position to convert.</param>
    /// <returns>A <see cref="Vector2"/> representing the screen coordinates corresponding to the given world position.</returns>
    public Vector2 GetWorldToScreen(Vector3 position) {
        Vector4 worldPosition = Vector4.Transform(new Vector4(position, 1.0F), this.GetView() * this.GetProjection());
        
        if (worldPosition.W != 0.0F) {
            worldPosition /= worldPosition.W;
        }
    
        return new Vector2(worldPosition.X, worldPosition.Y);
    }

    /// <summary>
    /// Updates the camera's projection matrix based on its current projection type,
    /// field of view, aspect ratio, and clipping planes.
    /// </summary>
    private void UpdateProjection() {
        switch (this.ProjectionType) {
            case ProjectionType.Perspective:
                this._projection = Matrix4x4.CreatePerspectiveFieldOfView(float.DegreesToRadians(this.Fov), this.AspectRatio, this.NearPlane, this.FarPlane);
                break;
            
            case ProjectionType.Orthographic:
                float top = this.Fov / 2.0F;
                float right = top * this.AspectRatio;
        
                this._projection = Matrix4x4.CreateOrthographicOffCenter(-right, right, -top, top, this.NearPlane, this.FarPlane);
                break;
        }
    }

    /// <summary>
    /// Updates the view matrix of the camera based on its current position, target, and up vector.
    /// </summary>
    private void UpdateView() {
        this._view = Matrix4x4.CreateLookAt(this.Position, this.Target, this.Up);
    }
} 