using System.Numerics;
using Bliss.CSharp;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Graphics.Rendering.Batches.Sprites;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Interact.Contexts;
using Bliss.CSharp.Textures;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Windowing;
using Veldrid;

namespace Bliss.Android.CSharp;

public class Game {
    
    public static Game Instance { get; private set; }
    public IWindow MainWindow { get; private set; }
    public GraphicsDevice GraphicsDevice { get; private set; }
    public CommandList CommandList { get; private set; }

    private BlissSurfaceView _surfaceView;

    private SpriteBatch _spriteBatch;
    private Texture2D _texture2D;

    public Game(BlissSurfaceView surfaceView) {
        Instance = this;
        this._surfaceView = surfaceView;
    }

    public void Run() {
        //Logger.Info("Hello World! Bliss start...");
        this.MainWindow = new Sdl3Window(this._surfaceView.Width, this._surfaceView.Height, "Android.Example", WindowState.Resizable);
        this._surfaceView.Resized += () => this.OnResize(new Rectangle((int) this._surfaceView.GetX(), (int) this._surfaceView.GetY(), this._surfaceView.Width, this._surfaceView.Height));
        this.GraphicsDevice = this._surfaceView.GraphicsDevice;

        //Logger.Info("Initialize command list...");
        this.CommandList = this.GraphicsDevice.ResourceFactory.CreateCommandList();
        
        //Logger.Info("Initialize global resources...");
        GlobalResource.Init(this.GraphicsDevice);
        
        //Logger.Info("Initialize input...");
        if (this.MainWindow is Sdl3Window) {
            Input.Init(new Sdl3InputContext(this.MainWindow));
        }
        else {
            throw new Exception("This type of window is not supported by the InputContext!");
        }
        
        //Logger.Info("Initialize audio device...");
        //AudioDevice.Init();

        this.Init();
        
        //Logger.Info("Start main loops...");
        this._surfaceView.RunContinuousRenderLoop();
        this._surfaceView.Rendering += () => {
            
            this.MainWindow.PumpEvents();
            Input.Begin();
            
            if (!this.MainWindow.Exists) {
                //break;
            }
            
            //AudioDevice.Update();
            this.Update();
            this.AfterUpdate();
            this.Draw();
            Input.End();
        };

        this._surfaceView.DeviceDisposed += () => {
            //Logger.Warn("Application shuts down!");
            this.OnClose();
        };
    }

    protected virtual void Init() {
        this._spriteBatch = new SpriteBatch(this.GraphicsDevice, this.MainWindow, this.GraphicsDevice.SwapchainFramebuffer.OutputDescription);
        this._texture2D = new Texture2D(this.GraphicsDevice, "content/images/logo.png");
    }

    protected virtual void Update() {
        
    }

    protected virtual void AfterUpdate() {
        
    }

    protected virtual void Draw() {
        this.CommandList.Begin();
        this.CommandList.SetFramebuffer(this.GraphicsDevice.SwapchainFramebuffer);
        this.CommandList.ClearColorTarget(0, Color.DarkGray.ToRgbaFloat());
        
        this._spriteBatch.DrawTexture(this._texture2D, new Vector2(20, 20));
        
        this.CommandList.End();
        this.GraphicsDevice.SubmitCommands(this.CommandList);
        this.GraphicsDevice.SwapBuffers(this._surfaceView.MainSwapchain);
    }
    
    protected virtual void OnClose() { }
    
    protected virtual void OnResize(Rectangle rectangle) {
        this.GraphicsDevice.MainSwapchain.Resize((uint) rectangle.Width, (uint) rectangle.Height);
    }
}