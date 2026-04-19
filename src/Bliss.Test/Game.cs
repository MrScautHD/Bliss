using System.Numerics;
using Bliss.CSharp;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Effects;
using Bliss.CSharp.Fonts;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Geometry.Animation;
using Bliss.CSharp.Geometry.Meshes;
using Bliss.CSharp.Geometry.Models;
using Bliss.CSharp.Graphics.Pipelines;
using Bliss.CSharp.Graphics.Pipelines.Buffers;
using Bliss.CSharp.Graphics.Rendering;
using Bliss.CSharp.Graphics.Rendering.Renderers;
using Bliss.CSharp.Graphics.Rendering.Renderers.Batches.Primitives;
using Bliss.CSharp.Graphics.Rendering.Renderers.Batches.Sprites;
using Bliss.CSharp.Graphics.Rendering.Renderers.Forward;
using Bliss.CSharp.Graphics.VertexTypes;
using Bliss.CSharp.Images;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Contexts;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Interact.Mice;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Materials;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Textures.Cubemaps;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using MiniAudioEx.Core.StandardAPI;
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
    
    public FullScreenRenderer FullScreenRenderer { get; private set; }
    public RenderTexture2D FullScreenTexture { get; private set; }
    public Texture2D FullScreenResolvedTexture { get; private set; }
    
    private BasicForwardRenderer _basicForwardRenderer;
    private List<Renderable> _renderables;
    
    private ImmediateRenderer _immediateRenderer;
    private SpriteBatch _spriteBatch;
    private PrimitiveBatch _primitiveBatch;
    private AnimatedImage _animatedImage;
    private Texture2D _gif;
    private Font _font;
    private Texture2D _logoTexture;
    private Cubemap _cubemap;
    private Texture2D _cubemapTexture;
    private Texture2D _button;
    
    private Cam3D _cam3D;
    private Model _playerModel;
    private Model _instancedPlayerModel;
    public BoundingBox _playerBox;
    private Model _planeModel;
    private Model _treeModel;
    private Model _cyberCarModel;
    private Texture2D _cyberCarTexture;

    private Texture2D _customMeshTexture;
    private Mesh<Vertex3D> _customPoly;
    private Mesh<Vertex3D> _customCube;
    private Mesh<Vertex3D> _customSphere;
    private Mesh<Vertex3D> _customHemishpere;
    private Mesh<Vertex3D> _customCylinder;
    private Mesh<Vertex3D> _customCapsule;
    private Mesh<Vertex3D> _customCone;
    private Mesh<Vertex3D> _customTorus;
    private Mesh<Vertex3D> _customKnot;
    private Mesh<Vertex3D> _customHeighmap;
    private Mesh<Vertex3D> _customQuad;
    //private Mesh _customCubemap;

    private int _frameCount;
    private bool _playingAnim;

    private string _textInput;
    
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
            SwapchainDepthFormat = PixelFormat.D32FloatS8UInt,
            SyncToVerticalBlank = this.Settings.VSync,
            ResourceBindingModel = ResourceBindingModel.Improved,
            PreferDepthRangeZeroToOne = true,
            PreferStandardClipSpaceYDirection = true,
            SwapchainSrgbFormat = false
        };
        
        this.MainWindow = Window.CreateWindow(WindowType.Sdl3, this.Settings.Size.Width, this.Settings.Size.Height, this.Settings.Title, this.Settings.WindowFlags, options, this.Settings.Backend, out GraphicsDevice graphicsDevice);
        this.MainWindow.SetMinimumSize(this.Settings.MinSize.Width, this.Settings.MinSize.Height);
        this.MainWindow.Resized += () => this.OnResize(new Rectangle(this.MainWindow.GetX(), this.MainWindow.GetY(), this.MainWindow.GetWidth(), this.MainWindow.GetHeight()));
        this.GraphicsDevice = graphicsDevice;
        
        Logger.Info("Loading window icon...");
        this.MainWindow.SetIcon(this.Settings.IconPath != string.Empty ? new Image(this.Settings.IconPath) : new Image("content/bliss/images/icon.png"));
        
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
        AudioContext.Initialize(44100, 2);

        this.Init();
        
        Logger.Info("Start main loops...");
        while (this.MainWindow.Exists) {
            if (this.GetTargetFps() != 0 && Time.Timer.Elapsed.TotalSeconds <= this._fixedFrameRate) {
                continue;
            }
            Time.Update();
            
            this.MainWindow.PumpEvents();
            Input.Begin();
            
            AudioContext.Update();
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
        this.FullScreenRenderer = new FullScreenRenderer(this.GraphicsDevice);
        this.FullScreenTexture = new RenderTexture2D(this.GraphicsDevice, (uint) this.MainWindow.GetWidth(), (uint) this.MainWindow.GetHeight(), false, this.Settings.SampleCount);
        this.FullScreenResolvedTexture = new Texture2D(this.GraphicsDevice, new Image(this.MainWindow.GetWidth(), this.MainWindow.GetHeight()), false);

        this._basicForwardRenderer = new BasicForwardRenderer(this.GraphicsDevice);
        this._renderables = new List<Renderable>();
        
        this._immediateRenderer = new ImmediateRenderer(this.GraphicsDevice);
        this._spriteBatch = new SpriteBatch(this.GraphicsDevice, this.MainWindow);
        this._primitiveBatch = new PrimitiveBatch(this.GraphicsDevice, this.MainWindow);
        
        this._font = new Font("content/fonts/fontoe.ttf");
        this._logoTexture = new Texture2D(this.GraphicsDevice, "content/bliss/images/logo.png");
        this._animatedImage = new AnimatedImage("content/animated.gif");
        this._gif = new Texture2D(this.GraphicsDevice, this._animatedImage.SpriteSheet);
        
        float aspectRatio = (float) this.MainWindow.GetWidth() / (float) this.MainWindow.GetHeight();
        this._cam3D = new Cam3D(this.GraphicsDevice, new Vector3(0, 3, -3), new Vector3(0, 1.5F, 0), aspectRatio);
        this._playerModel = Model.Load(this.GraphicsDevice, "content/player.glb", isSkinned: true);
        this._playerBox = this._playerModel.GenBoundingBox();
        
        foreach (IMesh mesh in this._playerModel.Meshes) {
            mesh.Material.RenderMode = RenderMode.Cutout;
            mesh.GenTangents();
        }
        
        // Instancing.
        this._instancedPlayerModel = Model.Load(this.GraphicsDevice, "content/player.glb", isSkinned: true);
        foreach (IMesh mesh in this._instancedPlayerModel.Meshes) {
            mesh.Material.Effect = GlobalResource.DefaultSkinnedModelEffect.GetEffectVariant(["USE_INSTANCING"]).Effect;
            mesh.Material.RenderMode = RenderMode.Cutout;

            if (mesh is Mesh<SkinnedVertex3D> skinnedMesh) {
                skinnedMesh.MeshData.VertexFormat = new VertexFormat(
                    SkinnedVertex3D.VertexLayout.Name,
                    [
                    ..SkinnedVertex3D.VertexLayout.Layouts,
                    ..SkinnedVertex3D.InstanceMatrixLayout.Layouts
                ]) {
                    IsSkinned = true
                };
            }
            
            mesh.GenTangents();
        }
        
        this._planeModel = Model.Load(this.GraphicsDevice, "content/plane.glb");
        this._treeModel = Model.Load(this.GraphicsDevice, "content/tree.glb", false);

        Texture2D treeTexture = new Texture2D(this.GraphicsDevice, "content/tree_texture.png");
        
        foreach (IMesh mesh in this._treeModel.Meshes) {
            mesh.Material.SetMapTexture(MaterialMapType.Albedo, treeTexture);
            mesh.Material.RasterizerState = RasterizerStateDescription.CULL_NONE;
            mesh.Material.RenderMode = RenderMode.Cutout;
        }
        
        this._cyberCarModel = Model.Load(this.GraphicsDevice, "content/cybercar.glb", false);
        this._cyberCarTexture = new Texture2D(this.GraphicsDevice, "content/cybercar.png");

        foreach (IMesh mesh in _cyberCarModel.Meshes) {
            mesh.Material.SetMapTexture(MaterialMapType.Albedo, this._cyberCarTexture);
            mesh.Material.RenderMode = RenderMode.Cutout;
        }
        
        // Make the blue window part translucent!
        this._cyberCarModel.Meshes[12].Material.BlendState = BlendStateDescription.SINGLE_ALPHA_BLEND;
        this._cyberCarModel.Meshes[12].Material.RenderMode = RenderMode.Translucent;
        
        this._customMeshTexture = new Texture2D(this.GraphicsDevice, "content/cube.png");
        
        this._customPoly = Mesh<Vertex3D>.GenPoly(this.GraphicsDevice, 40, 1);
        this._customPoly.Material.SetMapTexture(MaterialMapType.Albedo, this._customMeshTexture);

        this._customCube = Mesh<Vertex3D>.GenCube(this.GraphicsDevice, 1, 1, 1);
        this._customCube.Material.SetMapTexture(MaterialMapType.Albedo, this._customMeshTexture);

        this._customSphere = Mesh<Vertex3D>.GenSphere(this.GraphicsDevice, 1F, 40, 40);
        this._customSphere.Material.SetMapTexture(MaterialMapType.Albedo, this._customMeshTexture);

        this._customHemishpere = Mesh<Vertex3D>.GenHemisphere(this.GraphicsDevice, 1F, 40, 40);
        this._customHemishpere.Material.SetMapTexture(MaterialMapType.Albedo, this._customMeshTexture);

        this._customCylinder = Mesh<Vertex3D>.GenCylinder(this.GraphicsDevice, 1F, 1F, 40);
        this._customCylinder.Material.SetMapTexture(MaterialMapType.Albedo, this._customMeshTexture);
        
        this._customCapsule = Mesh<Vertex3D>.GenCapsule(this.GraphicsDevice, 1, 1, 60);
        this._customCapsule.Material.SetMapTexture(MaterialMapType.Albedo, this._customMeshTexture);

        this._customCone = Mesh<Vertex3D>.GenCone(this.GraphicsDevice, 1F, 1F, 40);
        this._customCone.Material.SetMapTexture(MaterialMapType.Albedo, this._customMeshTexture);

        this._customTorus = Mesh<Vertex3D>.GenTorus(this.GraphicsDevice, 2.0F, 1F, 40, 40);
        this._customTorus.Material.SetMapTexture(MaterialMapType.Albedo, this._customMeshTexture);

        this._customKnot = Mesh<Vertex3D>.GenKnot(this.GraphicsDevice, 1F, 1F, 40, 40);
        this._customKnot.Material.SetMapTexture(MaterialMapType.Albedo, this._customMeshTexture);
        
        this._customHeighmap = Mesh<Vertex3D>.GenHeightmap(this.GraphicsDevice, new Image("content/heightmap.png"), new Vector3(1, 1, 1));
        this._customHeighmap.Material.SetMapTexture(MaterialMapType.Albedo, new Texture2D(this.GraphicsDevice, "content/heightmap.png"));

        this._customQuad = Mesh<Vertex3D>.GenQuad(this.GraphicsDevice, 1, 1);
        this._customQuad.Material.SetMapTexture(MaterialMapType.Albedo, this._customMeshTexture);
        
        this._cubemap = new Cubemap(this.GraphicsDevice, "content/cubemap.png");
        this._cubemapTexture = new Texture2D(this.GraphicsDevice, this._cubemap.Images[5][0]);

        this._button = new Texture2D(this.GraphicsDevice, "content/button.png");
        
        this.SetupForwardRenderables();
    }
    
    protected virtual void Update() {
        if (Input.IsMouseButtonDoubleClicked(MouseButton.Left)) {
            Logger.Error("DOUBLE CLICKED!");
        }
        
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
        commandList.ClearDepthStencil(1.0F);
        
        // Enables relative mouse mod.
        Input.EnableRelativeMouseMode();
        
        // Drawing 3D.
        this._cam3D.Begin(this.CommandList);
        
        // ImmediateRenderer BEGIN
        this._immediateRenderer.Begin(commandList, this.FullScreenTexture.Framebuffer.OutputDescription);
        
        // Draw with texture.
        this._immediateRenderer.PushTexture(this._customMeshTexture);
        this._immediateRenderer.DrawCube(new Transform() { Translation = new Vector3(9, 0, 6) }, new Vector3(1, 1, 1));
        this._immediateRenderer.DrawSphere(new Transform() { Translation = new Vector3(13, 0, 6) }, 1, 20, 20);
        this._immediateRenderer.DrawHemisphere(new Transform() { Translation = new Vector3(17, 0, 6) }, 1, 20, 20);
        this._immediateRenderer.DrawCylinder(new Transform() { Translation = new Vector3(23, 0, 6) }, 1, 1, 20);
        this._immediateRenderer.DrawCylinder(new Transform() { Translation = new Vector3(23, 0, 6) }, 1, 1, 20);
        this._immediateRenderer.DrawCapsule(new Transform() { Translation = new Vector3(31, 0, 6) }, 1, 1, 20);
        this._immediateRenderer.DrawCone(new Transform() { Translation = new Vector3(38, 0, 6)}, 1, 1, 20);
        this._immediateRenderer.DrawTorus(new Transform() { Translation = new Vector3(42, 0, 6) }, 2, 1, 20, 20);
        this._immediateRenderer.DrawKnot(new Transform() { Translation = new Vector3(46, 0, 6) }, 1, 1, 20, 20);
        this._immediateRenderer.PopTexture();
        
        this._immediateRenderer.PushTexture(this._logoTexture);
        this._immediateRenderer.DrawBillboard(new Vector3(35, 0, 6));
        this._immediateRenderer.PopTexture();
        
        // Draw with wires.
        this._immediateRenderer.DrawCubeWires(new Transform() { Translation = new Vector3(11, 0, 6) }, new Vector3(1, 1, 1), Color.Green);
        this._immediateRenderer.DrawSphereWires(new Transform() { Translation = new Vector3(15, 0, 6) }, 1, 20, 20, Color.Green);
        this._immediateRenderer.DrawHemisphereWires(new Transform() { Translation = new Vector3(19, 0, 6) }, 1, 20, 20, Color.Green);
        this._immediateRenderer.DrawLine(new Vector3(20.5F, 0, 6), new Vector3(21.5F, 0, 6), Color.Green);
        this._immediateRenderer.DrawGrid(new Transform(), 96, 1, 16, Color.Gray);
        this._immediateRenderer.DrawCylinderWires(new Transform() { Translation = new Vector3(25, 0, 6) }, 1, 1, 20, Color.Green);
        this._immediateRenderer.DrawBoundingBox(new Transform() { Translation = new Vector3(28, 0, 6) }, this._playerBox, Color.Green);
        this._immediateRenderer.DrawCapsuleWires(new Transform() { Translation = new Vector3(33, 0, 6) }, 1, 1, 20, Color.Green);
        this._immediateRenderer.DrawConeWires(new Transform() { Translation = new Vector3(40, 0, 6)}, 1, 1, 20, Color.Green);
        this._immediateRenderer.DrawTorusWires(new Transform() { Translation = new Vector3(44, 0, 6) }, 2, 1, 20, 20, Color.Green);
        this._immediateRenderer.DrawKnotWires(new Transform() { Translation = new Vector3(48, 0, 6) }, 1, 1, 20, 20, Color.Green);
        
        this._immediateRenderer.End();
        // ImmediateRenderer END
        
        // Animate PLAYER (Begin)
        // Rest PLAYER animation.
        if (Input.IsKeyPressed(KeyboardKey.G)) {
            foreach (Renderable renderable in this._renderables) {
                
                // PLAYER
                foreach (IMesh mesh in this._playerModel.Meshes) {
                    if (renderable.Mesh == mesh) {
                        if (renderable.HasBones) {
                            renderable.ClearBoneMatrices();
                        }
                    }
                }
                
                // INSTANCED PLAYER
                foreach (IMesh mesh in this._instancedPlayerModel.Meshes) {
                    if (renderable.Mesh == mesh) {
                        if (renderable.HasBones) {
                            renderable.ClearBoneMatrices();
                        }
                    }
                }
            }
            
            this._playingAnim = false;
            Logger.Error("RESET ANIM");
        }
        
        // Play PLAYER animation.
        if (this._playingAnim) {
            foreach (Renderable renderable in this._renderables) {
                
                // PLAYER
                foreach (IMesh mesh in this._playerModel.Meshes) {
                    if (renderable.Mesh == mesh) {
                        ModelAnimation animation = this._playerModel.Animations[1];
                        
                        for (int boneId = 0; boneId < animation.BoneFrameTransformations[this._frameCount].Length; boneId++) {
                            if (renderable.HasBones) {
                                renderable.SetBoneMatrix(boneId, animation.BoneFrameTransformations[this._frameCount][boneId]);
                            }
                        }
                    }
                }
                
                // INSTANCED PLAYER
                foreach (IMesh mesh in this._instancedPlayerModel.Meshes) {
                    if (renderable.Mesh == mesh) {
                        ModelAnimation animation = this._instancedPlayerModel.Animations[1];
                        
                        for (int boneId = 0; boneId < animation.BoneFrameTransformations[this._frameCount].Length; boneId++) {
                            if (renderable.HasBones) {
                                renderable.SetBoneMatrix(boneId, animation.BoneFrameTransformations[this._frameCount][boneId]);
                            }
                        }
                    }
                }
            }
        }
        // Animate PLAYER (End)
        
        // DRAW FORWARD RENDERER (BEGIN)!
        // (Every renderable should be in his own (for example ModelRendererComponent, MeshRenderableComponent... (there are few concepts to handle this, bliss does not provide such a system!).
        foreach (Renderable renderable in this._renderables) {
            this._basicForwardRenderer.DrawRenderable(renderable);
        }
        
        this._basicForwardRenderer.Draw(commandList, this.FullScreenTexture.Framebuffer.OutputDescription);
        
        // DRAW FORWARD RENDERER (END)!
        
        this._cam3D.End();
        
        // SpriteBatch Drawing.
        this._spriteBatch.Begin(commandList, this.FullScreenTexture.Framebuffer.OutputDescription);
        
        if (Input.IsKeyPressed(KeyboardKey.O)) {
            Input.EnableTextInput();
        }

        if (Input.IsKeyPressed(KeyboardKey.Enter)) {
            Input.DisableTextInput();
        }

        if (Input.IsTextInputActive()) {
            if (Input.GetTypedText(out string text)) {
                this._textInput += text;
            }
            
            if (Input.IsKeyPressed(KeyboardKey.BackSpace, true)) {
                if (this._textInput.Length > 0) {
                    this._textInput = this._textInput.Remove(this._textInput.Length - 1, 1);
                }
            }
        }
        
        this._spriteBatch.DrawText(this._font, $"Text Input: {this._textInput}", new Vector2(80, 80), 18);
        
        this._spriteBatch.DrawText(this._font, $"FPS: {this.GetFps()}", new Vector2(5, 5), 18);
        
        int frameCount = this._animatedImage.GetFrameCount();
        int frame = (int) (Time.Total * 20) % frameCount;
        this._animatedImage.GetFrameInfo(frame, out int width, out int height, out float duration);
        
        // Calculate the position in the grid.
        int columns = this._animatedImage.Columns;
        int rows = this._animatedImage.Rows;
        
        int column = frame % columns;
        int row = (frame / columns) % rows;
        
        Rectangle sourceRect = new Rectangle(column * width, row * height, width, height);
        
        //this._spriteBatch.PushRasterizerState(this._spriteBatch.GetCurrentRasterizerState() with { ScissorTestEnabled = true });
        //this._spriteBatch.PushScissorRect(new Rectangle(30, 30, (int) (width / 2.0F * 0.2F), (int) (height / 2.0F * 0.2F)));
        this._spriteBatch.DrawTexture(this._gif, new Vector2(30, 30), sourceRect: sourceRect, scale: new Vector2(0.2F, 0.2F), color: new Color(255, 255, 255, 155));
        //this._spriteBatch.PopScissorRect();
        //this._spriteBatch.PopRasterizerState();
        
        //this._spriteBatch.DrawTexture(this._customMeshTexture, Input.GetMousePosition(), scale: new Vector2(3, 3));
        //this._spriteBatch.DrawTexture(this._button, new Vector2(300, 300), scale: new Vector2(3, 3));
        
        this._spriteBatch.End();
        
        this._primitiveBatch.Begin(commandList, this.FullScreenTexture.Framebuffer.OutputDescription);
        
        this._primitiveBatch.PushRasterizerState(this._primitiveBatch.GetCurrentRasterizerState() with { ScissorTestEnabled = true });
        this._primitiveBatch.PushScissorRect(new Rectangle(90, 90, 40, 80));
        this._primitiveBatch.DrawFilledCircle(new Vector2(130, 130), 40, 40, 0.5F, new Color(130, 130, 255, 120));
        this._primitiveBatch.PopScissorRect();
        this._primitiveBatch.PopRasterizerState();
        
        this._primitiveBatch.DrawFilledRectangle(new RectangleF(200, 200, 100, 100), origin: new Vector2(0, 0), rotation: _frameCount, color: Color.Green);
        this._primitiveBatch.DrawEmptyRectangle(new RectangleF(200, 200, 100, 100), 4, origin: new Vector2(0, 0), rotation: _frameCount, color: Color.Red);
        
        this._primitiveBatch.End();
        
        commandList.End();
        graphicsDevice.SubmitCommands(commandList);
        
        // Draw ScreenPass.
        commandList.Begin();
        
        // Resolve texture.
        if (this.FullScreenTexture.SampleCount != TextureSampleCount.Count1) {
            commandList.ResolveTexture(this.FullScreenTexture.ColorTexture, this.FullScreenResolvedTexture.DeviceTexture);
        }
        else {
            commandList.CopyTexture(this.FullScreenTexture.ColorTexture, this.FullScreenResolvedTexture.DeviceTexture);
        }
        
        commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        commandList.ClearColorTarget(0, Color.DarkGray.ToRgbaFloat());
        
        this.FullScreenRenderer.Draw(commandList, this.FullScreenResolvedTexture, this.GraphicsDevice.SwapchainFramebuffer.OutputDescription);
        
        commandList.End();
        
        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.SwapBuffers();
    }
    
    protected virtual void OnResize(Rectangle rectangle) {
        
        // Resize main swapchain.
        this.GraphicsDevice.MainSwapchain.Resize((uint) rectangle.Width, (uint) rectangle.Height);
        
        // Resize full screen texture.
        this.FullScreenTexture.Resize((uint) rectangle.Width, (uint) rectangle.Height);
        
        // Resize destination texture for MSAA.
        this.FullScreenResolvedTexture.Dispose();
        this.FullScreenResolvedTexture = new Texture2D(this.GraphicsDevice, new Image(rectangle.Width, rectangle.Height), false);
        
        // Resize cam.
        this._cam3D.Resize((uint) rectangle.Width, (uint) rectangle.Height);
    }
    
    protected virtual void OnClose() { }

    public int GetTargetFps() {
        return (int) (1.0F / this._fixedFrameRate);
    }

    public void SetTargetFps(int fps) {
        this._fixedFrameRate = 1.0F / fps;
    }

    private void SetupForwardRenderables() {
        
        // Meshes:
        this._renderables.Add(new Renderable(this._customPoly, new Transform() { Translation = new Vector3(9, 0, 0) }));
        this._renderables.Add(new Renderable(this._customCube, new Transform() { Translation = new Vector3(11, 0, 0) }));
        this._renderables.Add(new Renderable(this._customSphere, new Transform() { Translation = new Vector3(13, 0, 0) }));
        this._renderables.Add(new Renderable(this._customHemishpere, new Transform() { Translation = new Vector3(15, 0, 0) }));
        this._renderables.Add(new Renderable(this._customCylinder, new Transform() { Translation = new Vector3(17, 0, 0) }));
        this._renderables.Add(new Renderable(this._customCapsule, new Transform() { Translation = new Vector3(19, 0, 0) }));
        this._renderables.Add(new Renderable(this._customCone, new Transform() { Translation = new Vector3(21, 0, 0) }));
        this._renderables.Add(new Renderable(this._customTorus, new Transform() { Translation = new Vector3(23, 0, 0) }));
        this._renderables.Add(new Renderable(this._customKnot, new Transform() { Translation = new Vector3(25, 0, 0) }));
        this._renderables.Add(new Renderable(this._customHeighmap, new Transform() { Translation = new Vector3(27, 0, 0) }));
        this._renderables.Add(new Renderable(this._customQuad, new Transform() { Translation = new Vector3(29, 0, 0) }));
        
        // Models:
        foreach (IMesh mesh in this._planeModel.Meshes) {
            this._renderables.Add(new Renderable(mesh, new Transform()));
        }
        
        foreach (IMesh mesh in this._treeModel.Meshes) {
            this._renderables.Add(new Renderable(mesh, new Transform() { Translation = new Vector3(0, 0, 20) }));
        }
        
        foreach (IMesh mesh in this._cyberCarModel.Meshes) {
            this._renderables.Add(new Renderable(mesh, new Transform() { Translation = new Vector3(10, 0, 20) }));
        }
        
        foreach (IMesh mesh in this._playerModel.Meshes) {
            this._renderables.Add(new Renderable(mesh, new Transform() { Translation = new Vector3(0, 0.05F, 0) }));
        }
        
        foreach (IMesh mesh in this._instancedPlayerModel.Meshes) {
            List<Transform> transforms = new List<Transform>();
            
            for (int i = 0; i < 10; i++) {
                for (int j = 0; j < 10; j++) {
                    transforms.Add(new Transform() { Translation = new Vector3(-40 + (i * 2), 0, 0 + (j * 2))} );
                }
            }
            
            this._renderables.Add(new Renderable(mesh, transforms.ToArray(), false, true));
        }
    }
    
    private float _fpsTimer;
    private int _fpsFrames;
    private int _fps;

    private int GetFps() {
        this._fpsFrames++;
        this._fpsTimer += (float) Time.Delta;
        
        if (this._fpsTimer >= 0.25f) {
            this._fps = (int) (this._fpsFrames / this._fpsTimer);
            this._fpsFrames = 0;
            this._fpsTimer = 0;
        }
        
        return this._fps;
    }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this._playerModel.Dispose();
            this._planeModel.Dispose();
            this._treeModel.Dispose();
            this._font.Dispose();
            this._spriteBatch.Dispose();
            this._cam3D.Dispose();
            
            AudioContext.Deinitialize();
            GlobalResource.Destroy();
            Input.Destroy();
            this.MainWindow.Dispose();
            this.GraphicsDevice.Dispose();
        }
    }
}
