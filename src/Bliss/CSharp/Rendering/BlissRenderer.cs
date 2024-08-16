using System.Diagnostics;
using Bliss.CSharp.Vulkan;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Bliss.CSharp.Rendering;

public class BlissRenderer : Disposable {
    
    public readonly Vk Vk;
    public readonly BlissDevice Device;
    
    public bool IsFrameStarted { get; private set; }
    public int CurrentFrameIndex { get; private set; }
    
    private bool _framebufferResized;
    private uint _currentImageIndex;

    private readonly IView _window;
    
    private BlissSwapChain _swapChain;
    private CommandBuffer[] _commandBuffers;
    
    private bool _useFifo;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BlissRenderer"/> class.
    /// </summary>
    /// <param name="vk">The Vulkan instance.</param>
    /// <param name="window">The window interface.</param>
    /// <param name="device">The Vulkan device.</param>
    /// <param name="useFifo">Indicates whether to use FIFO for the swap chain.</param>
    public BlissRenderer(Vk vk, IView window, BlissDevice device, bool useFifo) {
        this.Vk = vk;
        this.Device = device;
        this._window = window;
        this._useFifo = useFifo;
        this.RecreateSwapChain();
        this.CreateCommandBuffers();
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use the First-In, First-Out (FIFO) mode for the swap chain.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if FIFO mode is used; otherwise, <see langword="false"/>.
    /// </value>
    public bool UseFifo {
        get => this._useFifo;
        set {
            this._useFifo = value;
            this._swapChain.UseFifo = value;
        }
    }

    /// <summary>
    /// Begins a new frame and returns the command buffer for recording rendering commands.
    /// </summary>
    /// <returns>The command buffer for recording rendering commands, or null if the swap chain needs to be recreated.</returns>
    public CommandBuffer? BeginFrame() {
        Debug.Assert(!this.IsFrameStarted, "Can't call beginFrame while already in progress!");
        Result result = this._swapChain.AcquireNextImage(ref this._currentImageIndex);

        if (result == Result.ErrorOutOfDateKhr) {
            this.RecreateSwapChain();
            return null;
        }
        else if (result != Result.Success && result != Result.SuboptimalKhr) {
            throw new Exception("Failed to acquire next swap chain image");
        }
        
        this.IsFrameStarted = true;

        CommandBuffer commandBuffer = this.GetCurrentCommandBuffer();

        CommandBufferBeginInfo beginInfo = new() {
            SType = StructureType.CommandBufferBeginInfo,
        };

        if (this.Vk.BeginCommandBuffer(commandBuffer, beginInfo) != Result.Success) {
            throw new Exception("Failed to begin recording command buffer!");
        }

        return commandBuffer;
    }

    /// <summary>
    /// Ends the current frame and submits the command buffers to the swap chain for rendering.
    /// </summary>
    public void EndFrame() {
        Debug.Assert(this.IsFrameStarted, "Can't call endFrame while frame is not in progress");

        CommandBuffer commandBuffer = this.GetCurrentCommandBuffer();

        if (this.Vk.EndCommandBuffer(commandBuffer) != Result.Success) {
            throw new Exception("Failed to record command buffer!");
        }

        Result result = this._swapChain.SubmitCommandBuffers(commandBuffer, this._currentImageIndex);
        
        if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || this._framebufferResized) {
            this._framebufferResized = false;
            this.RecreateSwapChain();
        }
        else if (result != Result.Success) {
            throw new Exception("Failed to submit command buffers");
        }

        this.IsFrameStarted = false;
        this.CurrentFrameIndex = (this.CurrentFrameIndex + 1) % this._swapChain.MaxFramesInFlight;
    }

    /// <summary>
    /// Begins rendering the swap chain render pass on the specified command buffer.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to begin the render pass on.</param>
    public unsafe void BeginSwapChainRenderPass(CommandBuffer commandBuffer) {
        Debug.Assert(this.IsFrameStarted, "Can't call beginSwapChainRenderPass if frame is not in progress");
        Debug.Assert(commandBuffer.Handle == this.GetCurrentCommandBuffer().Handle, "Can't begin render pass on command buffer from a different frame");

        RenderPassBeginInfo renderPassInfo = new() {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = this._swapChain.GetRenderPass(),
            Framebuffer = this._swapChain.GetSwapChainFrameBufferAt(this._currentImageIndex),
            RenderArea = new() {
                    Offset = new() {
                        X = 0,
                        Y = 0
                    },
                    Extent = this._swapChain.GetSwapChainExtent(),
                }
        };

        ClearValue[] clearValues = new ClearValue[] {
            new() {
                Color = new() {
                    Float32_0 = 0.01f,
                    Float32_1 = 0.01f,
                    Float32_2 = 0.01f,
                    Float32_3 = 1
                }
            },
            new() {
                DepthStencil = new() {
                    Depth = 1,
                    Stencil = 0
                }
            }
        };

        fixed (ClearValue* clearValuesPtr = clearValues) {
            renderPassInfo.ClearValueCount = (uint) clearValues.Length;
            renderPassInfo.PClearValues = clearValuesPtr;

            this.Vk.CmdBeginRenderPass(commandBuffer, &renderPassInfo, SubpassContents.Inline);
        }

        Viewport viewport = new() {
            X = 0.0f,
            Y = 0.0f,
            Width = this._swapChain.GetSwapChainExtent().Width,
            Height = this._swapChain.GetSwapChainExtent().Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        
        Rect2D scissor = new(new Offset2D(), this._swapChain.GetSwapChainExtent());
        this.Vk.CmdSetViewport(commandBuffer, 0, 1, &viewport);
        this.Vk.CmdSetScissor(commandBuffer, 0, 1, &scissor);
    }

    /// <summary>
    /// Ends the current swap chain render pass.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to end the render pass on.</param>
    public void EndSwapChainRenderPass(CommandBuffer commandBuffer) {
        Debug.Assert(this.IsFrameStarted, "Can't call endSwapChainRenderPass if frame is not in progress");
        Debug.Assert(commandBuffer.Handle == this.GetCurrentCommandBuffer().Handle, "Can't end render pass on command buffer from a different frame");

        this.Vk.CmdEndRenderPass(commandBuffer);
    }

    /// <summary>
    /// Retrieves the window extents in pixels.
    /// </summary>
    /// <returns>The window extents as an instance of Extent2D.</returns>
    private Extent2D GetWindowExtents() {
        return new Extent2D((uint) this._window.FramebufferSize.X, (uint) this._window.FramebufferSize.Y);
    }

    /// <summary>
    /// Retrieves the render pass associated with the swap chain.
    /// </summary>
    /// <returns>The render pass associated with the swap chain.</returns>
    public RenderPass GetSwapChainRenderPass() {
        return this._swapChain.GetRenderPass();
    }

    /// <summary>
    /// Calculates the aspect ratio of the swap chain.
    /// </summary>
    /// <returns>
    /// The aspect ratio of the swap chain as a floating-point value.
    /// The aspect ratio is calculated by dividing the width of the swap chain extent by its height.
    /// </returns>
    public float GetAspectRatio() {
        return this._swapChain.GetAspectRatio();
    }

    /// <summary>
    /// Retrieves the current command buffer for the current frame.
    /// </summary>
    /// <returns>The current command buffer.</returns>
    public CommandBuffer GetCurrentCommandBuffer() {
        return this._commandBuffers[this.CurrentFrameIndex];
    }

    /// <summary>
    /// Recreates the swap chain.
    /// </summary>
    private void RecreateSwapChain() {
        Vector2D<int> frameBufferSize = this._window.FramebufferSize;
        
        while (frameBufferSize.X == 0 || frameBufferSize.Y == 0) {
            frameBufferSize = this._window.FramebufferSize;
            this._window.DoEvents();
        }
        
        this.Vk.DeviceWaitIdle(this.Device.GetVkDevice());

        if (this._swapChain == null!) {
            this._swapChain = new BlissSwapChain(this.Vk, this.Device, this._useFifo, this.GetWindowExtents());
        }
        else {
            Format oldImageFormat = this._swapChain.SwapChainImageFormat;
            Format oldDepthFormat = this._swapChain.SwapChainDepthFormat;

            this._swapChain.Dispose();
            this._swapChain = new BlissSwapChain(this.Vk, this.Device, this._useFifo, this.GetWindowExtents());

            if (this._swapChain.SwapChainImageFormat != oldImageFormat || this._swapChain.SwapChainDepthFormat != oldDepthFormat) {
                throw new Exception("Swap chain image(or depth) format has changed!");
            }
        }
    }

    /// <summary>
    /// Creates the command buffers used for rendering.
    /// </summary>
    private unsafe void CreateCommandBuffers() {
        this._commandBuffers = new CommandBuffer[this._swapChain.GetImageCount()];

        CommandBufferAllocateInfo allocInfo = new() {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = this.Device.GetCommandPool(),
            CommandBufferCount = (uint) this._commandBuffers.Length
        };

        fixed (CommandBuffer* commandBuffersPtr = this._commandBuffers) {
            if (this.Vk.AllocateCommandBuffers(this.Device.GetVkDevice(), allocInfo, commandBuffersPtr) != Result.Success) {
                throw new Exception("Failed to allocate command buffers!");
            }
        }
    }

    /// <summary>
    /// Frees the allocated command buffers.
    /// </summary>
    private unsafe void FreeCommandBuffers() {
        fixed (CommandBuffer* commandBuffersPtr = this._commandBuffers) {
            this.Vk.FreeCommandBuffers(this.Device.GetVkDevice(), this.Device.GetCommandPool(), (uint) this._commandBuffers.Length, commandBuffersPtr);
        }
        
        Array.Clear(this._commandBuffers);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.FreeCommandBuffers();
        }
    }
}