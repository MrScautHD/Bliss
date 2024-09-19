using System.Numerics;
using Bliss.CSharp;
using Bliss.CSharp.Fonts;
using Bliss.CSharp.Graphics;
using Bliss.CSharp.Graphics.Rendering.Batches.Primitives;
using Bliss.CSharp.Graphics.Rendering.Batches.Sprites;
using Bliss.CSharp.Graphics.Rendering.Passes;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Keyboards;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Color = Bliss.CSharp.Colors.Color;
using RectangleF = Bliss.CSharp.Transformations.RectangleF;

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
    
    // TODO: Make Metal working for MacOS!
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
            PreferStandardClipSpaceYDirection = true,
            PreferDepthRangeZeroToOne = true,
            SyncToVerticalBlank = this.Settings.VSync
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
        this.CommandList = this.GraphicsDevice.ResourceFactory.CreateCommandList();
        
        Logger.Info("Initialize input...");
        Input.Init(this.MainWindow);
        
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

            this.Update();
            this.AfterUpdate();

            this._fixedUpdateTimer += Time.Delta;
            while (this._fixedUpdateTimer >= this._fixedUpdateTimeStep) {
                this.FixedUpdate();
                this._fixedUpdateTimer -= this._fixedUpdateTimeStep;
            }
            
            this.Draw(this.GraphicsDevice, this.CommandList);
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
    }

    protected virtual void Update() {
        if (Input.IsKeyPressed(KeyboardKey.A)) {
            Logger.Warn("A GOT PRESSED!!!!");
        }
        
        if (Input.IsKeyDown(KeyboardKey.D)) {
            Logger.Warn("D IS DOWN!!!!");
        }
        
        if (Input.IsKeyReleased(KeyboardKey.D)) {
            Logger.Warn("D IS Released!!!!");
        }
    }
    
    protected virtual void AfterUpdate() { }

    protected virtual void FixedUpdate() { }
    
    protected virtual void Draw(GraphicsDevice graphicsDevice, CommandList commandList) {
        commandList.Begin();
        commandList.SetFramebuffer(this.FullScreenTexture.Framebuffer);
        commandList.ClearColorTarget(0, Color.DarkGray.ToRgbaFloat());
        
        // PrimitiveBatch Drawing.
        this._primitiveBatch.Begin(commandList);
        
        // Draw Rectangle.
        RectangleF rectangle = new RectangleF(this.MainWindow.GetWidth() / 2.0F - 500, this.MainWindow.GetHeight() / 2.0F - 250, 1000, 500);
        this._primitiveBatch.DrawFilledRectangle(rectangle, default, 0, new Color(144, 238, 144, 20));
        this._primitiveBatch.DrawEmptyRectangle(rectangle, 4, default, 0, Color.DarkGreen);
        
        this._primitiveBatch.DrawFilledCircle(new Vector2(50, 50), 50, 60, Color.Red);
        
        this._primitiveBatch.End();
        
        // SpriteBatch Drawing.
        this._spriteBatch.Begin(commandList);
        
        // Draw FPS.
        this._spriteBatch.DrawText(this._font, $"FPS: {(int) (1.0F / Time.Delta)}", new Vector2(5, 5), 18);
        
        // Draw texture.
        Vector2 texturePos = new Vector2(this.MainWindow.GetWidth() / 2.0F - (216.0F / 4.0F / 2.0F), this.MainWindow.GetHeight() / 2.0F - (85.0F / 4.0F / 2.0F));
        Vector2 textureScale = new Vector2(4.0F, 4.0F);
        Vector2 textureOrigin = new Vector2(216.0F / 2.0F, 85.0F / 2.0F);
        this._spriteBatch.DrawTexture(this._texture, SamplerType.Point, texturePos, default, textureScale, textureOrigin, 10);
        
        // Draw text.
        string text = "This is my first FONT!!!";
        int textSize = 36;
        Vector2 measureTextSize = this._font.MeasureText(text, textSize);
        Vector2 textPos = new Vector2(this.MainWindow.GetWidth() / 2.0F - (measureTextSize.X / 2.0F), this.MainWindow.GetHeight() / 1.25F - (measureTextSize.Y / 2.0F));
        this._spriteBatch.DrawText(this._font, text, textPos, textSize);
        
        this._spriteBatch.End();
        
        commandList.End();
        graphicsDevice.SubmitCommands(commandList);
        
        // Draw ScreenPass.
        commandList.Begin();
        
        if (this.FullScreenTexture.SampleCount != TextureSampleCount.Count1) {
            commandList.ResolveTexture(this.FullScreenTexture.ColorTexture, this.FullScreenTexture.DestinationTexture);
        }
        
        commandList.SetFramebuffer(this.GraphicsDevice.SwapchainFramebuffer);
        commandList.ClearColorTarget(0, Color.DarkGray.ToRgbaFloat());
        
        this.FullScreenRenderPass.Draw(commandList, this.FullScreenTexture, SamplerType.Point);
        
        commandList.End();
        
        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.SwapBuffers();
    }
    
    protected virtual void OnResize(Rectangle rectangle) {
        this.GraphicsDevice.MainSwapchain.Resize((uint) rectangle.Width, (uint) rectangle.Height);
        this.FullScreenTexture.Resize((uint) rectangle.Width, (uint) rectangle.Height);
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
            this.GraphicsDevice.Dispose();
            this.MainWindow.Dispose();
            //TODO: Input.Destroy();
        }
    }
}