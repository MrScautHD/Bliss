using Bliss.CSharp;
using Bliss.CSharp.Interact;
using Bliss.CSharp.Logging;
using Bliss.CSharp.Rendering;
using Bliss.CSharp.Rendering.Vulkan;
using Bliss.CSharp.Rendering.Vulkan.Descriptor;
using Bliss.CSharp.Shaders;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using SilkWindow = Silk.NET.Windowing.Window;

namespace Bliss.Test;

public class Game : Disposable {
    
    public static Game Instance { get; private set; }
    
    public GameSettings Settings { get; private set; }
    
    public Vk Vk { get; private set; }
    public IWindow Window { get; private set; }
    public BlissDevice Device { get; private set; }
    public BlissRenderer Renderer { get; private set; }
    public BlissDescriptorPool GlobalPool { get; private set; }
    
    private readonly double _fixedTimeStep;
    private double _timer;

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
        this.Window.Render += this.Draw;
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
/*
        uint frames = BlissSwapChain.MaxDefaultFramesInFlight;
        ubos = new GlobalUbo[frames];
        uboBuffers = new BlissBuffer[frames];
        
        for (int i = 0; i < frames; i++) {
            ubos[i] = new GlobalUbo();
            uboBuffers[i] = new(this.Vk, this.Device, GlobalUbo.SizeOf(), 1, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
            uboBuffers[i].Map();
        }
        log.d("run", "initialized ubo buffers");

        globalSetLayout = new BlissDescriptorSetLayoutBuilder(this.Vk, this.Device)
            .AddBinding(0, DescriptorType.UniformBuffer, ShaderStageFlags.AllGraphics)
            .Build();

        globalDescriptorSets = new DescriptorSet[frames];
        for (var i = 0; i < globalDescriptorSets.Length; i++) {
            var bufferInfo = uboBuffers[i].DescriptorInfo();
            _ = new LveDescriptorSetWriter(vk, device, globalSetLayout)
                .WriteBuffer(0, bufferInfo)
                .Build(globalPool, globalSetLayout.GetDescriptorSetLayout(), ref globalDescriptorSets[i]);
        }
        log.d("run", "got globalDescriptorSets");

        simpleRenderSystem = new SimpleRenderSystem(
            vk, device,
            lveRenderer.GetSwapChainRenderPass(),
            globalSetLayout.GetDescriptorSetLayout()
        );

        pointLightRenderSystem = new(
            vk, device,
            lveRenderer.GetSwapChainRenderPass(),
            globalSetLayout.GetDescriptorSetLayout()
        );
        log.d("run", "got render systems");
        
        camera = new OrthographicCamera(Vector3.Zero, 4f, -20f, -140f, this.Window.FramebufferSize);
        //camera = new PerspectiveCamera(new Vector3(5,5,5), 45f, 0f, 0f, window.FramebufferSize);
        cameraController = new(camera, this.Window);
        resize(window.FramebufferSize);
        log.d("run", "got camera and controls");*/
        
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

    protected virtual void Init() { }

    protected virtual void Update(double delta) { }

    protected virtual void AfterUpdate(double delta) { }

    protected virtual void FixedUpdate() { }

    protected virtual void Draw(double delta) { }
    
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