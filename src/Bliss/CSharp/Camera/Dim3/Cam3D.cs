using System.Numerics;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Rendering;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Gamepads;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Mathematics;
using Veldrith;

namespace Bliss.CSharp.Camera.Dim3;

public class Cam3D : Disposable, ICam {
    
    /// <summary>
    /// References the currently active instance of the Cam3D class.
    /// Used to determine which camera is currently rendering the scene.
    /// Can be accessed from other classes to retrieve camera-specific properties and methods.
    /// </summary>
    public static Cam3D? ActiveCamera { get; private set; }
    
    /// <summary>
    /// Represents the graphics device associated with this camera instance.
    /// Provides access to rendering functionality and is responsible for managing GPU resources.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }
    
    /// <summary>
    /// Gets the aspect ratio (width / height) used to build the projection matrix.
    /// Updated through <see cref="Resize"/>.
    /// </summary>
    public float AspectRatio { get; private set; }
    
    /// <summary>
    /// The position of the camera in world space.
    /// </summary>
    public Vector3 Position;
    
    /// <summary>
    /// The point in world space the camera is looking at.
    /// </summary>
    public Vector3 Target;
    
    /// <summary>
    /// The upward direction of the camera. Defaults to (0,1,0) and is used for the view matrix, yaw rotation and vertical movement.
    /// </summary>
    public Vector3 Up;
    
    /// <summary>
    /// The projection type of the camera (Perspective or Orthographic).
    /// </summary>
    public ProjectionType ProjectionType;
    
    /// <summary>
    /// The movement mode of the camera (Free, Orbital, FirstPerson or ThirdPerson). Determines how input is handled in <see cref="Update"/>.
    /// </summary>
    public CameraMode Mode;
    
    /// <summary>
    /// The field of view in degrees for perspective projection.
    /// For orthographic projection, this value defines the height of the visible view volume.
    /// </summary>
    public float Fov;
    
    /// <summary>
    /// The distance to the near clipping plane. Must be positive and smaller than <see cref="FarPlane"/>.
    /// </summary>
    public float NearPlane;
    
    /// <summary>
    /// The distance to the far clipping plane. Must be positive and greater than <see cref="NearPlane"/>.
    /// </summary>
    public float FarPlane;
    
    /// <summary>
    /// Determines whether the camera may be rotated by input in <see cref="Update"/>.
    /// </summary>
    public bool AllowRotation;
    
    /// <summary>
    /// Determines whether the camera may be moved by input in <see cref="Update"/>.
    /// </summary>
    public bool AllowMovement;
    
    /// <summary>
    /// The mouse sensitivity, in degrees of rotation per pixel of mouse movement.
    /// </summary>
    public float MouseSensitivity;
    
    /// <summary>
    /// The gamepad stick sensitivity multiplier. A value of 1.0 rotates the camera by 100 degrees per second at full stick deflection.
    /// </summary>
    public float GamepadSensitivity;
    
    /// <summary>
    /// The movement speed of the camera in world units per second.
    /// </summary>
    public float MovementSpeed;
    
    /// <summary>
    /// The rotation speed of the camera around its target in <see cref="CameraMode.Orbital"/> mode, in radians per second.
    /// </summary>
    public float OrbitalSpeed;
    
    /// <summary>
    /// The view frustum of the camera, extracted from the view and projection matrices in <see cref="Begin"/>.
    /// </summary>
    private Frustum _frustum;
    
    /// <summary>
    /// The cached projection matrix, recalculated in <see cref="Begin"/>.
    /// </summary>
    private Matrix4x4 _projection;
    
    /// <summary>
    /// The cached view matrix, recalculated in <see cref="Begin"/>.
    /// </summary>
    private Matrix4x4 _view;
    
    /// <summary>
    /// The uniform buffer that holds the projection matrix (index 0) and the view matrix (index 1) for the vertex shader stage.
    /// </summary>
    private SimpleUniformBuffer<Matrix4x4> _matrixBuffer;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Cam3D"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering operations.</param>
    /// <param name="position">The camera position in world space.</param>
    /// <param name="target">The target point the camera is looking at.</param>
    /// <param name="aspectRatio">The aspect ratio (width / height). Must be greater than zero.</param>
    /// <param name="up">The upward direction vector. Defaults to (0,1,0).</param>
    /// <param name="projectionType">The type of projection (Perspective or Orthographic).</param>
    /// <param name="mode">The camera movement mode.</param>
    /// <param name="fov">The field of view in degrees. Should be between 1 and 179.</param>
    /// <param name="nearPlane">The near clipping plane. Must be positive and smaller than <paramref name="farPlane"/>.</param>
    /// <param name="farPlane">The far clipping plane. Must be positive and greater than <paramref name="nearPlane"/>.</param>
    public Cam3D(GraphicsDevice graphicsDevice, Vector3 position, Vector3 target, float aspectRatio, Vector3? up = null, ProjectionType projectionType = ProjectionType.Perspective, CameraMode mode = CameraMode.Free, float fov = 70.0F, float nearPlane = 0.01F, float farPlane = 10000.0F) {
        this.GraphicsDevice = graphicsDevice;
        this.Position = position;
        this.Target = target;
        this.AspectRatio = aspectRatio;
        this.Up = up ?? Vector3.UnitY;
        this.ProjectionType = projectionType;
        this.Mode = mode;
        this.Fov = fov;
        this.NearPlane = nearPlane;
        this.FarPlane = farPlane;
        this.AllowRotation = true;
        this.AllowMovement = true;
        this.MouseSensitivity = 0.05F;
        this.GamepadSensitivity = 1.0F;
        this.MovementSpeed = 10.0F;
        this.OrbitalSpeed = 0.5F;
        this._frustum = new Frustum();
        this._matrixBuffer = new SimpleUniformBuffer<Matrix4x4>(GlobalResource.GraphicsDevice, 2, ShaderStages.Vertex);
        this._matrixBuffer.DeviceBuffer.Name = "MatrixBuffer";
    }
    
    /// <summary>
    /// Updates the camera based on the current <see cref="Mode"/>, handling mouse, keyboard and gamepad input for rotation and movement.
    /// </summary>
    /// <param name="delta">The time elapsed since the last update, in seconds.</param>
    public void Update(double delta) {
        switch (this.Mode) {
            case CameraMode.Free: {
                if (!Input.GetFirstAvailableGamepad(out uint gamepad)) {
                    if (this.AllowRotation) {
                        this.SetYaw(this.GetYaw() - (Input.GetMouseDelta().X * this.MouseSensitivity), false);
                        this.SetPitch(this.GetPitch() - (Input.GetMouseDelta().Y * this.MouseSensitivity), false);
                    }
                    
                    if (this.AllowMovement) {
                        if (Input.IsKeyDown(KeyboardKey.W)) {
                            this.MoveForward(this.MovementSpeed * (float) delta, true);
                        }
                        
                        if (Input.IsKeyDown(KeyboardKey.S)) {
                            this.MoveForward(-this.MovementSpeed * (float) delta, true);
                        }
                        
                        if (Input.IsKeyDown(KeyboardKey.A)) {
                            this.MoveRight(-this.MovementSpeed * (float) delta, true);
                        }
                        
                        if (Input.IsKeyDown(KeyboardKey.D)) {
                            this.MoveRight(this.MovementSpeed * (float) delta, true);
                        }
                        
                        if (Input.IsKeyDown(KeyboardKey.Space)) {
                            this.MoveUp(this.MovementSpeed * (float) delta);
                        }
                        
                        if (Input.IsKeyDown(KeyboardKey.ShiftLeft)) {
                            this.MoveUp(-this.MovementSpeed * (float) delta);
                        }
                    }
                }
                else {
                    if (this.AllowRotation) {
                        this.SetYaw(this.GetYaw() - Input.GetGamepadAxisMovement(gamepad, GamepadAxis.RightX) * this.GamepadSensitivity * 100 * (float)delta, false);
                        this.SetPitch(this.GetPitch() - Input.GetGamepadAxisMovement(gamepad, GamepadAxis.RightY) * this.GamepadSensitivity * 100 * (float)delta, false);
                    }
                    
                    if (this.AllowMovement) {
                        this.MoveForward(this.MovementSpeed * Input.GetGamepadAxisMovement(gamepad, GamepadAxis.TriggerRight) * (float)delta, false);
                        this.MoveForward(-this.MovementSpeed * Input.GetGamepadAxisMovement(gamepad, GamepadAxis.TriggerLeft) * (float)delta, false);
                        
                        if (Input.IsGamepadButtonDown(gamepad, GamepadButton.RightShoulder)) {
                            this.MoveRight(this.MovementSpeed * (float)delta, true);
                        }
                        
                        if (Input.IsGamepadButtonDown(gamepad, GamepadButton.LeftShoulder)) {
                            this.MoveRight(-this.MovementSpeed * (float)delta, true);
                        }
                        
                        if (Input.IsGamepadButtonDown(gamepad, GamepadButton.RightStick)) {
                            this.MoveUp(this.MovementSpeed * (float)delta);
                        }
                        
                        if (Input.IsGamepadButtonDown(gamepad, GamepadButton.LeftStick)) {
                            this.MoveUp(-this.MovementSpeed * (float)delta);
                        }
                    }
                }
                
                break;
            }
            case CameraMode.Orbital: {
                if (this.AllowRotation) {
                    Matrix4x4 rotation = Matrix4x4.CreateFromAxisAngle(this.Up, this.OrbitalSpeed * (float) delta);
                    Vector3 view = this.Position - this.Target;
                    Vector3 transform = Vector3.Transform(view, rotation);
                    this.Position = this.Target + transform;
                }
                
                if (this.AllowMovement) {
                    if (Input.IsMouseScrolling(out Vector2 wheelDelta)) {
                        this.MoveToTarget(-wheelDelta.Y);
                    }
                }
                
                break;
            }
            case CameraMode.FirstPerson: {
                if (this.AllowRotation) {
                    if (!Input.GetFirstAvailableGamepad(out uint gamepad)) {
                        this.SetYaw(this.GetYaw() - (Input.GetMouseDelta().X * this.MouseSensitivity), false);
                        this.SetPitch(this.GetPitch() - (Input.GetMouseDelta().Y * this.MouseSensitivity), false);
                    }
                    else {
                        this.SetYaw(this.GetYaw() - Input.GetGamepadAxisMovement(gamepad, GamepadAxis.RightX) * this.GamepadSensitivity * 100 * (float)delta, false);
                        this.SetPitch(this.GetPitch() - Input.GetGamepadAxisMovement(gamepad, GamepadAxis.RightY) * this.GamepadSensitivity * 100 * (float)delta, false);
                    }
                }
                
                break;
            }
            case CameraMode.ThirdPerson: {
                if (this.AllowRotation) {
                    if (!Input.GetFirstAvailableGamepad(out uint gamepad)) {
                        this.SetYaw(this.GetYaw() - (Input.GetMouseDelta().X * this.MouseSensitivity), true);
                        this.SetPitch(this.GetPitch() - (Input.GetMouseDelta().Y * this.MouseSensitivity), true);
                    }
                    else {
                        this.SetYaw(this.GetYaw() + Input.GetGamepadAxisMovement(gamepad, GamepadAxis.RightX) * this.GamepadSensitivity * 100 * (float)delta, true);
                        this.SetPitch(this.GetPitch() + Input.GetGamepadAxisMovement(gamepad, GamepadAxis.RightY) * this.GamepadSensitivity * 100 * (float)delta, true);
                    }
                }
                
                break;
            }
        }
    }

    /// <summary>
    /// Resizes the viewport and updates the aspect ratio based on the given width and height.
    /// </summary>
    /// <param name="width">The new width of the viewport.</param>
    /// <param name="height">The new height of the viewport.</param>
    public void Resize(uint width, uint height) {
        this.AspectRatio = (float) width / (float) height;
    }

    /// <summary>
    /// Activates the current camera instance as the active camera and updates its projection and view matrices.
    /// </summary>
    /// <param name="commandList">The <see cref="CommandList"/> used to issue GPU commands for updating camera matrices and related operations.</param>
    public void Begin(CommandList commandList) {
        
        // Update proj/view.
        this.UpdateProjection();
        this.UpdateView();
        
        // Upload proj/view to GPU.
        this._matrixBuffer.SetValue(0, this.GetProjection());
        this._matrixBuffer.SetValue(1, this.GetView());
        this._matrixBuffer.UpdateBufferDeferred(commandList);
        
        // Extract frustum.
        this._frustum.Extract(this.GetView() * this.GetProjection());
        
        // Set active camera.
        ActiveCamera = this;
    }

    /// <summary>
    /// Ends the current 3D rendering session and deactivates the camera.
    /// </summary>
    public void End() {
        ActiveCamera = null;
    }

    /// <summary>
    /// Gets the matrix buffer associated with the camera.
    /// </summary>
    /// <returns> A <see cref="SimpleUniformBuffer{Matrix4x4}"/> containing the camera's transformation matrices. </returns>
    public SimpleUniformBuffer<Matrix4x4> GetMatrixBuffer() {
        return this._matrixBuffer;
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
        Vector3 rotation = Quaternion.ToEulerAngles(Quaternion.CreateFromRotationMatrix(this.GetView()));
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
        Vector3 targetPosition = Vector3.RotateByAxisAngle(view, this.Up, finalAngle);
        
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
        float maxAngleUp = Vector3.AngleBetween(this.Up, view) - 0.001F;
        float maxAngleDown = -Vector3.AngleBetween(Vector3.Negate(this.Up), view) + 0.001F;
        
        // Clamp the angle within the min and max bounds.
        finalAngle = Math.Clamp(finalAngle, maxAngleDown, maxAngleUp);
        
        // Calculate target pos.
        Vector3 targetPosition = Vector3.RotateByAxisAngle(view, this.GetRight(), finalAngle);
        
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
        this.Up = Vector3.RotateByAxisAngle(this.Up, this.GetForward(), finalAngle);
    }
    
    /// <summary>
    /// Retrieves the camera frustum by extracting the frustum planes based on the current view and projection matrices.
    /// </summary>
    /// <returns>The updated frustum containing the extracted planes.</returns>
    public Frustum GetFrustum() {
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
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._matrixBuffer.Dispose();
        }
    }
}