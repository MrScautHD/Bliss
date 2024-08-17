using Bliss.CSharp;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Shaders;
using Bliss.CSharp.Windowing;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

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
        
        this.Window = new Window(this.Settings.Width, this.Settings.Height, this.Settings.Title, options, VeldridStartup.GetPlatformDefaultBackend(), out GraphicsDevice graphicsDevice);
        this.GraphicsDevice = graphicsDevice;
        
        Logger.Info("Initialize time...");
        Time.Init();
        
        Logger.Info($"Set target FPS to: {this.Settings.TargetFps}");
        this.SetTargetFps(this.Settings.TargetFps);
        
        Logger.Info("Create CommandList");
        this.CommandList = this.GraphicsDevice.ResourceFactory.CreateCommandList();
        
        this.Init();
        
        Logger.Info("Start main Loops...");
        while (Window.Exists) {
            if (this.GetTargetFps() != 0 && Time.Timer.Elapsed.TotalSeconds <= this._fixedFrameRate) {
                continue;
            }
            Time.Update();
            
            this.Window.PumpEvents();
            Sdl2Events.ProcessEvents();
            
            this.Update();
            this.AfterUpdate();

            this._fixedUpdateTimer += Time.Delta;
            while (this._fixedUpdateTimer >= this._fixedUpdateTimeStep) {
                this.FixedUpdate();
                this._fixedUpdateTimer -= this._fixedUpdateTimeStep;
            }
            
            this.Draw(this.GraphicsDevice, this.CommandList);
        }
        
        Logger.Warn("Application shuts down!");
        this.OnClose();
    }

    protected virtual void Init() {
        (Shader, Shader) shader = ShaderHelper.Load(this.GraphicsDevice.ResourceFactory, "content/shaders/default_shader.vert", "content/shaders/default_shader.frag");

    }

    protected virtual void Update() { }

    protected virtual void AfterUpdate() { }

    protected virtual void FixedUpdate() { }

    protected virtual void Draw(GraphicsDevice graphicsDevice, CommandList commandList) {
        //commandList.Begin();
        //
        //commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        //commandList.ClearColorTarget(0, RgbaFloat.Grey);
        //
        //commandList.SetVertexBuffer(0, this._vertexBuffer);
        //commandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt16);
        //commandList.SetPipeline(this.Pipeline);
        //commandList.SetGraphicsResourceSet(0, this.ResourceSet);
        //
        //commandList.DrawIndexed(
        //    indexCount: 4,
        //    instanceCount: 1,
        //    indexStart: 0,
        //    vertexOffset: 0,
        //    instanceStart: 0);
        //
        //commandList.End();
        //graphicsDevice.SubmitCommands(commandList);
        //
        //graphicsDevice.SwapBuffers();
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
        }
    }
}