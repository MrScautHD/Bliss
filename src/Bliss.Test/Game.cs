using System.Numerics;
using Bliss.CSharp;
using Bliss.CSharp.Audio;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Fonts;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Graphics;
using Bliss.CSharp.Graphics.Rendering.Batches.Primitives;
using Bliss.CSharp.Graphics.Rendering.Batches.Sprites;
using Bliss.CSharp.Graphics.Rendering.Passes;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Contexts;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Color = Bliss.CSharp.Colors.Color;
using Rectangle = Bliss.CSharp.Transformations.Rectangle;

namespace Bliss.Test;

public class Game : Disposable {
    
    public static Game Instance { get; private set; }
    public GameSettings Settings { get; private set; }

    public IWindow MainWindow { get; private set; }
    public GraphicsDevice GraphicsDevice { get; private set; }
    public CommandList CommandList { get; private set; }

    private double _fixedFrameRate;
    
    private readonly double _fixedUpdateTimeStep;
    private double _fixedUpdateTimer;
    
    public FullScreenRenderPass FullScreenRenderPass { get; private set; }
    public RenderTexture2D FullScreenTexture { get; private set; }

    private SpriteBatch _spriteBatch;
    private PrimitiveBatch _primitiveBatch;
    private Texture2D _texture;
    private Font _font;
    
    private Cam3D _cam3D;
    private Model _playerModel;
    private Model _planeModel;
    
    private Mesh _customPoly;
    private Mesh _customCube;
    private Mesh _customSphere;
    private Mesh _customHemishpere;
    private Mesh _customCylinder;
    private Mesh _customCone;
    private Mesh _customTorus;
    private Mesh _customKnot;
    private Mesh _customHeighmap;
    private Mesh _customCubemap;

    private int _frameCount;
    private bool _playingAnim;
    
    public Game(GameSettings settings) {
        Instance = this;
        this.Settings = settings;
        this._fixedUpdateTimeStep = settings.FixedTimeStep;
    }

    public void Run() {
        Logger.Info("Hello World! Bliss start...");
        Logger.Info($"\t> CPU: {SystemInfo.Cpu}");
        Logger.Info($"\t> MEMORY: {SystemInfo.MemorySize} GB");
        Logger.Info($"\t> THREADS: {SystemInfo.Threads}");
        Logger.Info($"\t> OS: {SystemInfo.Os}");
        
        Logger.Info("Initialize window and graphics device...");
        GraphicsDeviceOptions options = new GraphicsDeviceOptions() {
            Debug = false,
            HasMainSwapchain = true,
            SwapchainDepthFormat = null,
            SyncToVerticalBlank = this.Settings.VSync,
            ResourceBindingModel = ResourceBindingModel.Improved,
            PreferDepthRangeZeroToOne = true,
            PreferStandardClipSpaceYDirection = true,
            SwapchainSrgbFormat = false
        };
        
        this.MainWindow = Window.CreateWindow(WindowType.Sdl3, this.Settings.Width, this.Settings.Height, this.Settings.Title, this.Settings.WindowFlags, options, this.Settings.Backend, out GraphicsDevice graphicsDevice);
        this.MainWindow.Resized += () => this.OnResize(new Rectangle(this.MainWindow.GetX(), this.MainWindow.GetY(), this.MainWindow.GetWidth(), this.MainWindow.GetHeight()));
        this.GraphicsDevice = graphicsDevice;
        
        Logger.Info("Loading window icon...");
        this.MainWindow.SetIcon(this.Settings.IconPath != string.Empty ? Image.Load<Rgba32>(this.Settings.IconPath) : Image.Load<Rgba32>("content/images/icon.png"));
        
        Logger.Info("Initialize time...");
        Time.Init();
        
        Logger.Info($"Set target FPS to: {this.Settings.TargetFps}");
        this.SetTargetFps(this.Settings.TargetFps);
        
        Logger.Info("Initialize command list...");
        this.CommandList = graphicsDevice.ResourceFactory.CreateCommandList();
        
        Logger.Info("Initialize global resources...");
        GlobalResource.Init(graphicsDevice);
        
        Logger.Info("Initialize input...");
        if (this.MainWindow is Sdl3Window) {
            Input.Init(new Sdl3InputContext(this.MainWindow));
        }
        else {
            throw new Exception("This type of window is not supported by the InputContext!");
        }
        
        Logger.Info("Initialize audio device...");
        AudioDevice.Init();

        this.Init();
        
        Logger.Info("Start main loops...");
        while (this.MainWindow.Exists) {
            if (this.GetTargetFps() != 0 && Time.Timer.Elapsed.TotalSeconds <= this._fixedFrameRate) {
                continue;
            }
            Time.Update();
            
            this.MainWindow.PumpEvents();
            Input.Begin();
            
            if (!this.MainWindow.Exists) {
                break;
            }
            
            AudioDevice.Update();
            this.Update();
            this.AfterUpdate();

            this._fixedUpdateTimer += Time.Delta;
            while (this._fixedUpdateTimer >= this._fixedUpdateTimeStep) {
                this.FixedUpdate();
                this._fixedUpdateTimer -= this._fixedUpdateTimeStep;
            }
            
            this.Draw(graphicsDevice, this.CommandList);
            Input.End();
        }
        
        Logger.Warn("Application shuts down!");
        this.OnClose();
    }
    
    protected virtual void Init() {
        this.FullScreenRenderPass = new FullScreenRenderPass(this.GraphicsDevice, this.GraphicsDevice.SwapchainFramebuffer.OutputDescription);
        this.FullScreenTexture = new RenderTexture2D(this.GraphicsDevice, (uint) this.MainWindow.GetWidth(), (uint) this.MainWindow.GetHeight(), this.Settings.SampleCount);
        
        this._spriteBatch = new SpriteBatch(this.GraphicsDevice, this.MainWindow, this.FullScreenTexture.Framebuffer.OutputDescription);
        this._primitiveBatch = new PrimitiveBatch(this.GraphicsDevice, this.MainWindow, this.FullScreenTexture.Framebuffer.OutputDescription);
        this._texture = new Texture2D(this.GraphicsDevice, "content/images/logo.png");
        this._font = new Font("content/fonts/fontoe.ttf");
        
        this._cam3D = new Cam3D((uint) this.MainWindow.GetWidth(), (uint) this.MainWindow.GetHeight(), new Vector3(0, 3, -3), new Vector3(0, 1.5F, 0), default, ProjectionType.Perspective, CameraMode.Free);
        this._playerModel = Model.Load(this.GraphicsDevice, "content/player.glb");
        this._planeModel = Model.Load(this.GraphicsDevice, "content/plane.glb");

        Texture2D customMeshTexture = new Texture2D(this.GraphicsDevice, "content/cube.png");

        this._customPoly = Mesh.GenPoly(this.GraphicsDevice, 9, 1);
        this._customPoly.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), customMeshTexture);

        this._customCube = Mesh.GenCube(this.GraphicsDevice, 1, 1, 1);
        this._customCube.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), customMeshTexture);

        this._customSphere = Mesh.GenSphere(this.GraphicsDevice, 1F, 40, 40);
        this._customSphere.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), customMeshTexture);

        this._customHemishpere = Mesh.GenHemisphere(this.GraphicsDevice, 1F, 40, 40);
        this._customHemishpere.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), customMeshTexture);

        this._customCylinder = Mesh.GenCylinder(this.GraphicsDevice, 1F, 1F, 40);
        this._customCylinder.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), customMeshTexture);

        this._customCone = Mesh.GenCone(this.GraphicsDevice, 1F, 1F, 40);
        this._customCone.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), customMeshTexture);

        this._customTorus = Mesh.GenTorus(this.GraphicsDevice, 2.0F, 1F, 40, 40);
        this._customTorus.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), customMeshTexture);

        this._customKnot = Mesh.GenKnot(this.GraphicsDevice, 1F, 1F, 40, 40);
        this._customKnot.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), customMeshTexture);
        
        this._customHeighmap = Mesh.GenHeightmap(this.GraphicsDevice, Image.Load<Rgba32>("content/heightmap.png"), new Vector3(10, 10, 10));
        this._customHeighmap.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), customMeshTexture);

        this._customCubemap = Mesh.GenCubemap(this.GraphicsDevice, Image.Load<Rgba32>("content/cubemap.png"), Vector3.One);
        this._customCubemap.Material.SetMapTexture(MaterialMapType.Albedo.GetName(), customMeshTexture);
    }

    protected virtual void Update() {
        this._cam3D.Update((float) Time.Delta);
    }
    
    protected virtual void AfterUpdate() { }
    
    protected virtual void FixedUpdate() {
        if (Input.IsKeyDown(KeyboardKey.H)) {
            this._playingAnim = true;
            this._frameCount++;

            if (this._frameCount >= this._playerModel.Animations[1].FrameCount) {
                this._frameCount = 0;
            }
        }
    }
    
    protected virtual void Draw(GraphicsDevice graphicsDevice, CommandList commandList) {
        commandList.Begin();
        commandList.SetFramebuffer(this.FullScreenTexture.Framebuffer);
        commandList.ClearColorTarget(0, Color.DarkGray.ToRgbaFloat());
        
        // Enables relative mouse mod.
        Input.EnableRelativeMouseMode();

        // Drawing 3D.
        this._cam3D.Begin(commandList);
        
        this._customPoly.Draw(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(7, 0, 0)}, Color.White);
        this._customCube.Draw(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(9, 0, 0)}, Color.White);
        this._customSphere.Draw(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(13, 0, 0)}, Color.White);
        this._customHemishpere.Draw(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(15, 0, 0)}, Color.White);
        this._customCylinder.Draw(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(17, 0, 0)}, Color.White);
        this._customCone.Draw(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(19, 0, 0)}, Color.White);
        this._customTorus.Draw(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(21, 0, 0)}, Color.White);
        this._customKnot.Draw(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(23, 0, 0)}, Color.White);
        this._customHeighmap.Draw(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(26, 0, 0)}, Color.White);
        this._customCubemap.Draw(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(34, 0, 0)}, Color.White);

        if (this._cam3D.GetFrustum().ContainsBox(this._planeModel.BoundingBox)) {
            this._planeModel.Draw(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform(), Color.White);
        }
        
        if (Input.IsKeyPressed(KeyboardKey.G)) {
            this._playerModel.ResetAnimationBones(commandList);
            this._playingAnim = false;
            Logger.Error("RESET ANIM");
        }

        if (this._cam3D.GetFrustum().ContainsBox(this._playerModel.BoundingBox)) {
            if (this._playingAnim) {
                this._playerModel.UpdateAnimationBones(commandList, this._playerModel.Animations[1], this._frameCount);
            }
            this._playerModel.Draw(commandList, this.FullScreenTexture.Framebuffer.OutputDescription, new Transform() { Translation = new Vector3(0, 0.05F, 0)}, Color.White);
        }
        
        this._cam3D.End();
        
        // SpriteBatch Drawing.
        this._spriteBatch.Begin(commandList);
        this._spriteBatch.DrawText(this._font, $"FPS: {(int) (1.0F / Time.Delta)}", new Vector2(5, 5), 18);
        this._spriteBatch.End();
        
        commandList.End();
        graphicsDevice.SubmitCommands(commandList);
        
        // Draw ScreenPass.
        commandList.Begin();
        
        if (this.FullScreenTexture.SampleCount != TextureSampleCount.Count1) {
            commandList.ResolveTexture(this.FullScreenTexture.ColorTexture, this.FullScreenTexture.DestinationTexture);
        }
        
        commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        commandList.ClearColorTarget(0, Color.DarkGray.ToRgbaFloat());
        
        this.FullScreenRenderPass.Draw(commandList, this.FullScreenTexture, SamplerType.Point);
        
        commandList.End();
        
        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.SwapBuffers();
    }
    
    protected virtual void OnResize(Rectangle rectangle) {
        this.GraphicsDevice.MainSwapchain.Resize((uint) rectangle.Width, (uint) rectangle.Height);
        this.FullScreenTexture.Resize((uint) rectangle.Width, (uint) rectangle.Height);
        this._cam3D.Resize((uint) rectangle.Width, (uint) rectangle.Height);
    }

    protected virtual void OnClose() { }

    public int GetTargetFps() {
        return (int) (1.0F / this._fixedFrameRate);
    }

    public void SetTargetFps(int fps) {
        this._fixedFrameRate = 1.0F / fps;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._playerModel.Dispose();
            this._planeModel.Dispose();
            
            AudioDevice.Destroy();
            GlobalResource.Destroy();
            Input.Destroy();
            this.MainWindow.Dispose();
            this.GraphicsDevice.Dispose();
        }
    }
}