using System.Numerics;
using Bliss.CSharp;
using Bliss.CSharp.Camera.Dim3;
using Bliss.CSharp.Colors;
using Bliss.CSharp.Geometry;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Rendering;
using Bliss.CSharp.Rendering.Systems;
using Bliss.CSharp.Shaders;
using Bliss.CSharp.Transformations;
using Bliss.CSharp.Vulkan;
using Bliss.CSharp.Vulkan.Descriptor;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using SilkWindow = Silk.NET.Windowing.Window;

namespace Bliss.Test;

// TODO CHECK THIS: https://github.com/dotnet/Silk.NET/blob/main/examples/CSharp/OpenGL%20Tutorials/Tutorial%204.1%20-%20Model%20Loading/Model.cs

public class Game : Disposable {
    
    public static Game Instance { get; private set; }
    
    public GameSettings Settings { get; private set; }
    
    public Vk Vk { get; private set; }
    public IWindow Window { get; private set; }
    public BlissDevice Device { get; private set; }
    public BlissRenderer Renderer { get; private set; }
    
    public GlobalUbo[] GlobalUbos { get; private set; }
    public BlissBuffer[] GlobalUboBuffers { get; private set; }
    
    public BlissDescriptorPool GlobalPool { get; private set; }
    public BlissDescriptorSetLayout GlobalSetLayout { get; private set; }
    public DescriptorSet[] GlobalDescriptorSets { get; private set; }
    
    public SimpleRenderSystem SimpleRenderSystem { get; private set; }
    public Cam3D Cam3D { get; private set; } // TODO REMOVE IT FROM HERE
    
    private readonly double _fixedTimeStep;
    private double _timer;

    public Renderable[] Renderables;

    public Game(GameSettings settings) {
        Instance = this;
        this.Settings = settings;
        this._fixedTimeStep = settings.FixedTimeStep;
    }

    public void Run() {
        Logger.Info("Hello World! Bliss start...");
        
        Logger.Info("Initialize Vulkan...");
        this.Vk = Vk.GetApi();
        
        Logger.Info("Initialize Window...");
        this.Window = SilkWindow.Create(WindowOptions.DefaultVulkan with {
            Title = this.Settings.Title,
            Size = new Vector2D<int>(this.Settings.Width, this.Settings.Height)
        });
        
        this.Window.Update += this.RunLoop;
        this.Window.Render += this.RunDrawing;
        this.Window.Closing += this.Close;
        
        this.Window.Initialize();
        
        if (this.Window.VkSurface == null) {
            throw new PlatformNotSupportedException("Windowing platform doesn't support Vulkan.");
        }
        
        Logger.Info("Initialize Device...");
        this.Device = new BlissDevice(this.Vk, this.Window);

        Logger.Info("Initialize Renderer...");
        this.Renderer = new BlissRenderer(this.Vk, this.Window, this.Device, this.Settings.UseFifo);

        Logger.Info("Initialize Global Pool...");
        this.GlobalPool = new BlissDescriptorPoolBuilder(this.Vk, this.Device)
            .SetMaxSets(BlissSwapChain.MaxDefaultFramesInFlight)
            .AddSize(DescriptorType.UniformBuffer, BlissSwapChain.MaxDefaultFramesInFlight)
            .Build();
        
        Logger.Info("Initialize Input...");
        Input.Init(this.Window);
        
        this.Init();

        Logger.Info("Initialize Global Ubo buffers...");
        uint frames = BlissSwapChain.MaxDefaultFramesInFlight;
        
        this.GlobalUbos = new GlobalUbo[frames];
        this.GlobalUboBuffers = new BlissBuffer[frames];
        
        for (int i = 0; i < frames; i++) {
            this.GlobalUbos[i] = new GlobalUbo();
            this.GlobalUboBuffers[i] = new(this.Vk, this.Device, GlobalUbo.SizeOf(), 1, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
            this.GlobalUboBuffers[i].Map();
        }

        Logger.Info("Initialize Global Set Layout...");
        this.GlobalSetLayout = new BlissDescriptorSetLayoutBuilder(this.Vk, this.Device)
            .AddBinding(0, DescriptorType.UniformBuffer, ShaderStageFlags.AllGraphics)
            .Build();

        Logger.Info("Initialize Global Descriptor Sets...");
        this.GlobalDescriptorSets = new DescriptorSet[frames];
        
        for (var i = 0; i < this.GlobalDescriptorSets.Length; i++) {
            _ = new BlissDescriptorSetWriter(this.Vk, this.Device, this.GlobalSetLayout).WriteBuffer(0, this.GlobalUboBuffers[i].DescriptorInfo()).Build(this.GlobalPool, ref this.GlobalDescriptorSets[i]);
        }

        Logger.Info("Initialize Render System...");
        this.SimpleRenderSystem = new SimpleRenderSystem(this.Vk, this.Device, this.Renderer.GetSwapChainRenderPass(), this.GlobalSetLayout.DescriptorSetLayout);
        
        Logger.Info("Setup Camera (TEMP get removed from here!)");
        this.Cam3D = new Cam3D(this.Window, new Vector3(3, 3, 3), new Vector3(0, 2, 0));
        
        Logger.Info("Start main Loops...");
        this.Window.Run();
        this.Vk.DeviceWaitIdle(this.Device.GetVkDevice());
    }
    
    protected virtual void RunLoop(double delta) {
        Input.BeginInput();
        
        this.Update(delta);
        this.AfterUpdate(delta);
        
        this._timer += delta;
        while (this._timer >= this._fixedTimeStep) {
            this.FixedUpdate();
            this._timer -= this._fixedTimeStep;
        }
        
        Input.EndInput();
    }

    protected virtual void RunDrawing(double delta) {
        CommandBuffer? commandBuffer = this.Renderer.BeginFrame();

        if (commandBuffer != null) {
            int frameIndex = this.Renderer.CurrentFrameIndex;

            FrameInfo frameInfo = new() {
                FrameIndex = frameIndex,
                FrameTime = (float) delta,
                CommandBuffer = commandBuffer.Value,
                Camera = this.Cam3D,
                GlobalDescriptorSet = this.GlobalDescriptorSets[frameIndex],
                RenderableObjects = this.Renderables
            };
            
            this.GlobalUbos[frameIndex].Update(this.Cam3D.GetProjection(), this.Cam3D.GetView(), new Vector4(this.Cam3D.GetForward(), 0));
            this.GlobalUboBuffers[frameIndex].WriteBytesToBuffer(this.GlobalUbos[frameIndex].AsBytes());
            
            this.Renderer.BeginSwapChainRenderPass(commandBuffer.Value);
            
            this.Draw(frameInfo, delta);
            
            this.Renderer.EndSwapChainRenderPass(commandBuffer.Value);
            this.Renderer.EndFrame();
        }
    }

    protected virtual void Init() {
        this.Renderables = new Renderable[1];
        this.Renderables[0] = new Renderable(Model.Load(this.Vk, this.Device, "content/player.glb"), Color.White, new Transform() {
            Translation = new Vector3(0, 0.5F, 0),
            Rotation = Quaternion.Identity,
            Scale = new Vector3(1, 1, 1)
        });
    }

    protected virtual void Update(double delta) { }

    protected virtual void AfterUpdate(double delta) { }

    protected virtual void FixedUpdate() { }

    protected virtual void Draw(FrameInfo frameInfo, double delta) {
        this.SimpleRenderSystem.Draw(frameInfo); // Replace the simple RenderSystem with Graphics.BeginMode3D(); AND Graphics.BeginShader();
    }
    
    protected virtual void Close() { }
    
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.Window.Dispose();
            this.Renderer.Dispose();
            this.GlobalPool.Dispose();
            this.Device.Dispose();
            this.Vk.Dispose();
        }
    }
}