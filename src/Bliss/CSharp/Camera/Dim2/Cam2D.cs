using System.Numerics;
using Veldrid;

namespace Bliss.CSharp.Camera.Dim2;

public class Cam2D : ICam {
    
    /// <summary>
    /// Gets or sets the active camera instance.
    /// </summary>
    public static Cam2D? ActiveCamera { get; private set; }

    /// <summary>
    /// Gets the current viewport settings of the camera.
    /// </summary>
    public Viewport Viewport { get; private set; }

    /// <summary>
    /// Defines the camera's follow mode, determining how the camera follows its target.
    /// </summary>
    public CameraFollowMode Mode;

    /// <summary>
    /// Gets or sets the position of the camera in 2D space.
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// Gets or sets the target position of the camera.
    /// </summary>
    public Vector2 Target;

    /// <summary>
    /// Gets or sets the offset position of the camera.
    /// </summary>
    public Vector2 Offset;

    /// <summary>
    /// Gets or sets the rotation angle of the camera, in degrees.
    /// </summary>
    public float Rotation;

    /// <summary>
    /// Gets or sets the zoom level of the camera.
    /// </summary>
    public float Zoom;

    /// <summary>
    /// Represents the minimum speed at which the camera follows its target.
    /// </summary>
    public float MinFollowSpeed;

    /// <summary>
    /// Represents the minimum distance at which the follow effect is activated.
    /// </summary>
    public float MinFollowEffectLength;

    /// <summary>
    /// Represents the fraction of the distance to the target that the camera covers per update cycle, used to determine the speed of the camera's follow movement.
    /// </summary>
    public float FractionFollowSpeed;

    /// <summary>
    /// Stores the view matrix of the camera.
    /// </summary>
    private Matrix4x4 _view;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Cam2D"/> class, setting up the camera with the specified
    /// dimensions, follow mode, position, target position, and optional offset, rotation, and zoom level.
    /// Additional properties are set to control camera follow speed and effect length.
    /// </summary>
    /// <param name="width">The width of the camera view.</param>
    /// <param name="height">The height of the camera view.</param>
    /// <param name="mode">The camera follow mode, determining how the camera follows the target.</param>
    /// <param name="position">The initial position of the camera.</param>
    /// <param name="target">The target position for the camera to focus on.</param>
    /// <param name="offset">An optional offset from the target position. Defaults to zero vector.</param>
    /// <param name="rotation">The rotation of the camera in degrees. Default is 0.0F.</param>
    /// <param name="zoom">The zoom level of the camera. Default is 1.0F.</param>
    public Cam2D(uint width, uint height, CameraFollowMode mode, Vector2 position, Vector2 target, Vector2? offset = null, float rotation = 0.0F, float zoom = 1.0F) {
        this.Mode = mode;
        this.Position = position;
        this.Target = target;
        this.Offset = offset ?? Vector2.Zero;
        this.Rotation = rotation;
        this.Zoom = zoom;
        this.MinFollowSpeed = 30;
        this.MinFollowEffectLength = 10;
        this.FractionFollowSpeed = 0.8F;
        this.Resize(width, height);
    }

    /// <summary>
    /// Updates the camera's state based on the elapsed time and current mode.
    /// </summary>
    /// <param name="timeStep">The time elapsed since the last update, in seconds.</param>
    public void Update(double timeStep) {
        switch (this.Mode) {
            case CameraFollowMode.FollowTarget:
                this.TargetFollowMovement();
                break;
            
            case CameraFollowMode.FollowTargetSmooth:
                this.SmoothTargetFollowMovement(timeStep);
                break;
        }
    }

    /// <summary>
    /// Resizes the camera's viewport to the specified width and height.
    /// </summary>
    /// <param name="width">The new width of the viewport.</param>
    /// <param name="height">The new height of the viewport.</param>
    public void Resize(uint width, uint height) {
        this.Viewport = new Viewport(0, 0, width, height, 0, 0);
    }

    /// <summary>
    /// Initializes the camera's usage in the current frame and sets it as the active camera.
    /// </summary>
    /// <param name="commandList">The command list to begin the camera's drawing operations.</param>
    public void Begin(CommandList commandList) {
        this.UpdateView();
        ActiveCamera = this;
    }

    /// <summary>
    /// Ends the current camera session, setting the active camera to null.
    /// </summary>
    public void End() {
        ActiveCamera = null;
    }
    
    /// <summary>
    /// Retrieves the current view matrix of the camera.
    /// </summary>
    /// <returns>The <see cref="Matrix4x4"/> representing the camera's view matrix.</returns>
    public Matrix4x4 GetView() {
        return this._view;
    }

    /// <summary>
    /// Converts a screen position to a world position based on the camera's current view matrix.
    /// </summary>
    /// <param name="position">The screen position to be converted.</param>
    /// <returns>The corresponding world position.</returns>
    public Vector2 GetScreenToWorld(Vector2 position) {
        Matrix4x4.Invert(this._view, out Matrix4x4 result);
        return Vector2.Transform(position, result);
    }

    /// <summary>
    /// Converts a given world position to screen coordinates based on the camera's current view matrix.
    /// </summary>
    /// <param name="position">The world position to convert.</param>
    /// <returns>A <see cref="Vector2"/> representing the screen coordinates corresponding to the given world position.</returns>
    public Vector2 GetWorldToScreen(Vector2 position) {
        return Vector2.Transform(position, this._view);
    }

    /// <summary>
    /// Updates the camera's view matrix based on its current position, rotation, zoom, and offset parameters.
    /// </summary>
    private void UpdateView() {
        Matrix4x4 origin = Matrix4x4.CreateTranslation(-this.Position.X, -this.Position.Y, 0.0F);
        Matrix4x4 rotation = Matrix4x4.CreateRotationZ(float.DegreesToRadians(this.Rotation));
        Matrix4x4 scale = Matrix4x4.CreateScale(this.Zoom, this.Zoom, 1.0F);
        Matrix4x4 translation = Matrix4x4.CreateTranslation(this.Offset.X, this.Offset.Y, 0.0F);
        
        this._view = origin * rotation * scale * translation;
    }

    /// <summary>
    /// Adjusts the camera offset to center the target within the viewport.
    /// </summary>
    private void TargetFollowMovement() {
        this.Offset = new Vector2(this.Viewport.Width / 2.0F, this.Viewport.Height / 2.0F);
        this.Position = this.Target;
    }

    /// <summary>
    /// Smoothly follows the target by adjusting the camera position based on the difference between the current and previous target positions,
    /// scaled by a follow speed factor. The camera movement speed is determined by the distance to the target and the specified time step.
    /// </summary>
    /// <param name="timeStep">The elapsed time since the last update, used to scale the movement speed.</param>
    private void SmoothTargetFollowMovement(double timeStep) {
        this.Offset = new Vector2(this.Viewport.Width / 2.0F, this.Viewport.Height / 2.0F);
        Vector2 diff = this.Target - this.Position;
        float length = diff.Length();
        
        if (length > this.MinFollowEffectLength) {
            float speed = Math.Max(this.FractionFollowSpeed * length, this.MinFollowSpeed);
            this.Position += diff * (speed * (float) timeStep / length);
        }
    }
}