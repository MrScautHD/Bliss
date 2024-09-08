using System.Numerics;
using Bliss.CSharp;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Fonts;
using Bliss.CSharp.Graphics;
using Bliss.CSharp.Graphics.Rendering.Sprites;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Windowing;
using FontStashSharp;
using Veldrid;
using Veldrid.Sdl2;

namespace Bliss.Test;

public class Game : Disposable {
    
    public static Game Instance { get; private set; }
    public GameSettings Settings { get; private set; }

    public Window Window { get; private set; }
    public GraphicsDevice GraphicsDevice { get; private set; }
    public CommandList CommandList { get; private set; }

    private double _fixedFrameRate;
    
    private readonly double _fixedUpdateTimeStep;
    private double _fixedUpdateTimer;

    private SpriteBatch _spriteBatch;
    private Texture2D _texture;
    private Font _font;
    
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
            PreferDepthRangeZeroToOne = true
        };
        
        this.Window = new Window(this.Settings.Width, this.Settings.Height, this.Settings.Title, options, this.Settings.Backend, out GraphicsDevice graphicsDevice);
        this.GraphicsDevice = graphicsDevice;
        
        Logger.Info("Initialize time...");
        Time.Init();
        
        Logger.Info($"Set target FPS to: {this.Settings.TargetFps}");
        this.SetTargetFps(this.Settings.TargetFps);
        
        Logger.Info("Initialize command list...");
        this.CommandList = this.GraphicsDevice.ResourceFactory.CreateCommandList();
        
        Logger.Info("Initialize input...");
        Input.Init(this.Window);
        
        this.Init();
        
        Logger.Info("Start main loops...");
        while (this.Window.Exists) {
            if (this.GetTargetFps() != 0 && Time.Timer.Elapsed.TotalSeconds <= this._fixedFrameRate) {
                continue;
            }
            Time.Update();
            
            Sdl2Events.ProcessEvents();
            Input.Begin(this.Window.PumpEvents());
            
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
        this._spriteBatch = new SpriteBatch(this.GraphicsDevice, this.Window);
        this._texture = new Texture2D(this.GraphicsDevice, "content/images/icon.png");
        this._font = new Font("content/fonts/fontoe.ttf");
    }

    protected virtual void Update() { }
    
    protected virtual void AfterUpdate() { }

    protected virtual void FixedUpdate() { }
    
    protected virtual void Draw(GraphicsDevice graphicsDevice, CommandList commandList) {
        commandList.Begin();
        commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        commandList.ClearColorTarget(0, Color.DarkGray.ToRgbaFloat());

        this._spriteBatch.Begin(commandList, blendState: BlendState.AdditiveBlend);
        
        // Draw texture.
        this._spriteBatch.DrawTexture(this._texture, SamplerType.Point, new Vector2(this.Window.Width / 2.0F - (216.0F / 4 / 2.0F), this.Window.Height / 2.0F - (85.0F / 4 / 2.0F)), default, new Vector2(4, 4), new Vector2(216.0F / 2.0F, 85.0F / 2.0F), 10, Color.White, SpriteFlip.None);
        
        // Draw text.
        int textSize = 36;
        string text = "This is my first FONT!!!";
        Vector2 measureTextSize = this._font.MeasureText(text, textSize);
        this._spriteBatch.DrawText(this._font, text, new Vector2(this.Window.Width / 2.0F - (measureTextSize.X / 2.0F), this.Window.Height / 1.25F - (measureTextSize.Y / 2.0F)), textSize);
        
        this._spriteBatch.End();
        
        commandList.End();
        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.SwapBuffers();
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
            this.Window.Close();
            Input.Destroy();
        }
    }
}