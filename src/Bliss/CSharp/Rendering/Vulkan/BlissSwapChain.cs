using Bliss.CSharp.Logging;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Bliss.CSharp.Rendering.Vulkan;

public class BlissSwapChain : Disposable {
    
    public readonly static int MaxFramesInFlight = 2;

    public readonly Vk Vk;
    
    public readonly BlissDevice Device;
    public readonly Device VkDevice;
    
    public Format SwapChainImageFormat { get; private set; }
    public Format SwapChainDepthFormat { get; private set; }
    
    public bool UseFifo;
    
    private Extent2D _windowExtent;

    private KhrSwapchain _khrSwapChain;
    private SwapchainKHR _swapChain; // TODO: Get Method (GetVkSwapChain).

    private Image[] _swapChainImages;
    
    private Extent2D _swapChainExtent; // TODO: Get Method (GetSwapChainExtent, GetSize, GetAspectRatio => (float) _swapChainExtent.Width / (float)_swapChainExtent.Height)
    
    private ImageView[] _swapChainImageViews; // TODO: Get Method (GetSwapChainImageViews, GetImageCount).
    private Framebuffer[] _swapChainFrameBuffers; // TODO: Get Method (GetSwapChainFrameBuffers, GetSwapChainFrameBufferAt, GetSwapChainFrameBufferCount).
    
    private RenderPass _renderPass; // TODO: Get Method (GetRenderPass).

    private Image[] _depthImages;
    private DeviceMemory[] _depthImageMemories;
    private ImageView[] _depthImageViews;

    private Image[] _colorImages;
    private DeviceMemory[] _colorImageMemories;
    private ImageView[] _colorImageViews;
    
    private Semaphore[] _imageAvailableSemaphores;
    private Semaphore[] _renderFinishedSemaphores;
    private Fence[] _inFlightFences;

    private Fence[] _imagesInFlight;
    private int _currentFrame;

    private BlissSwapChain? _oldSwapChain;

    public BlissSwapChain(Vk vk, BlissDevice device, bool useFifo, Extent2D extent) {
        this.Vk = vk;
        this.Device = device;
        this.VkDevice = device.GetVkDevice();
        this.UseFifo = useFifo;
        this._windowExtent = extent;
        this.Setup();
    }

    private void Setup() {
        this.CreateSwapChain();
        this.CreateImageViews();
        this.CreateRenderPass();
        this.CreateColorResources();
        this.CreateDepthResources();
        this.CreateFrameBuffers();
        this.CreateSyncObjects();
    }

    public Result AcquireNextImage(ref uint imageIndex) {
        this.Vk.WaitForFences(this.Device.GetVkDevice(), 1, this._inFlightFences[this._currentFrame], true, ulong.MaxValue);
        
        return this._khrSwapChain.AcquireNextImage(this.Device.GetVkDevice(), this._swapChain, ulong.MaxValue, this._imageAvailableSemaphores[this._currentFrame], default, ref imageIndex);
    }

    public unsafe Result SubmitCommandBuffers(CommandBuffer commandBuffer, uint imageIndex) {
        if (this._imagesInFlight[imageIndex].Handle != default) {
            this.Vk.WaitForFences(this.Device.GetVkDevice(), 1, this._imagesInFlight[imageIndex], true, ulong.MaxValue);
        }
        
        this._imagesInFlight[imageIndex] = this._inFlightFences[this._currentFrame];

        SubmitInfo submitInfo = new() {
            SType = StructureType.SubmitInfo,
        };

        Semaphore* waitSemaphores = stackalloc[] {
            this._imageAvailableSemaphores[this._currentFrame]
        };
        
        PipelineStageFlags* waitStages = stackalloc[] {
            PipelineStageFlags.ColorAttachmentOutputBit
        };
        
        submitInfo = submitInfo with {
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,

            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };

        Semaphore* signalSemaphores = stackalloc[] {
            this._renderFinishedSemaphores[this._currentFrame]
        };
        
        submitInfo = submitInfo with {
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores,
        };

        this.Vk.ResetFences(this.Device.GetVkDevice(), 1, this._inFlightFences[this._currentFrame]);

        if (this.Vk.QueueSubmit(this.Device.GetGraphicsQueue(), 1, submitInfo, this._inFlightFences[this._currentFrame]) != Result.Success) {
            throw new Exception("Failed to submit draw command buffer!");
        }

        SwapchainKHR* swapChains = stackalloc[] {
            this._swapChain
        };
        
        PresentInfoKHR presentInfo = new() {
            SType = StructureType.PresentInfoKhr,

            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,

            SwapchainCount = 1,
            PSwapchains = swapChains,

            PImageIndices = &imageIndex
        };
        
        this._currentFrame = (this._currentFrame + 1) % MaxFramesInFlight;

        return this._khrSwapChain.QueuePresent(this.Device.GetPresentQueue(), presentInfo);
    }

    private unsafe void CreateSwapChain() {
        BlissDevice.SwapChainSupportDetails swapChainSupport = this.Device.QuerySwapChainSupport();

        SurfaceFormatKHR surfaceFormat = this.ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        PresentModeKHR presentMode = this.ChoosePresentMode(swapChainSupport.PresentModes);
        Extent2D extent = this.ChooseSwapExtent(swapChainSupport.Capabilities);

        uint imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
        
        if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount) {
            imageCount = swapChainSupport.Capabilities.MaxImageCount;
        }

        SwapchainCreateInfoKHR creatInfo = new() {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = this.Device.GetSurface(),

            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit
        };

        BlissDevice.QueueFamilyIndices indices = this.Device.FindQueueFamilies();
        
        uint* queueFamilyIndices = stackalloc[] {
            indices.GraphicsFamily!.Value,
            indices.PresentFamily!.Value
        };

        if (indices.GraphicsFamily != indices.PresentFamily) {
            creatInfo = creatInfo with {
                ImageSharingMode = SharingMode.Concurrent,
                QueueFamilyIndexCount = 2,
                PQueueFamilyIndices = queueFamilyIndices
            };
        }
        else {
            creatInfo.ImageSharingMode = SharingMode.Exclusive;
        }

        creatInfo = creatInfo with {
            PreTransform = swapChainSupport.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true
        };

        if (this._khrSwapChain == null) {
            if (!this.Vk.TryGetDeviceExtension(this.Device.GetInstance(), this.VkDevice, out this._khrSwapChain)) {
                throw new NotSupportedException("VK_KHR_swapchain extension not found.");
            }
        }

        creatInfo.OldSwapchain = this._oldSwapChain?._swapChain ?? default;

        if (this._khrSwapChain.CreateSwapchain(this.VkDevice, creatInfo, null, out this._swapChain) != Result.Success) {
            throw new Exception("Failed to create swap chain!");
        }

        this._khrSwapChain.GetSwapchainImages(this.VkDevice, this._swapChain, ref imageCount, null);
        this._swapChainImages = new Image[imageCount];
        
        fixed (Image* swapChainImagesPtr = this._swapChainImages) {
            this._khrSwapChain.GetSwapchainImages(this.VkDevice, this._swapChain, ref imageCount, swapChainImagesPtr);
        }

        this.SwapChainImageFormat = surfaceFormat.Format;
        this._swapChainExtent = extent;
    }

    private unsafe void CreateImageViews() {
        this._swapChainImageViews = new ImageView[this._swapChainImages.Length];

        for (int i = 0; i < this._swapChainImages.Length; i++) {
            ImageViewCreateInfo createInfo = new() {
                SType = StructureType.ImageViewCreateInfo,
                Image = this._swapChainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = this.SwapChainImageFormat,
                SubresourceRange = new() {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                }
            };

            if (this.Vk.CreateImageView(this.VkDevice, createInfo, null, out this._swapChainImageViews[i]) != Result.Success) {
                throw new Exception("Failed to create image view!");
            }
        }
    }
    
    private unsafe void CreateRenderPass() {
        AttachmentDescription depthAttachment = new() {
            Format = this.Device.FindDepthFormat(),
            Samples = this.Device.MsaaSamples,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
        };

        AttachmentReference depthAttachmentRef = new() {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal
        };

        AttachmentDescription colorAttachment = new() {
            Format = this.SwapChainImageFormat,
            Samples = this.Device.MsaaSamples,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.ColorAttachmentOptimal
        };

        AttachmentReference colorAttachmentRef = new() {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };
        
        AttachmentDescription colorAttachmentResolve = new() {
            Format = this.SwapChainImageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.DontCare,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr
        };

        AttachmentReference colorAttachmentResolveRef = new() {
            Attachment = 2,
            Layout = ImageLayout.AttachmentOptimalKhr
        };

        SubpassDescription subpass = new() {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
            PDepthStencilAttachment = &depthAttachmentRef,
            PResolveAttachments = &colorAttachmentResolveRef
        };

        SubpassDependency dependency = new() {
            DstSubpass = 0,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            SrcSubpass = Vk.SubpassExternal,
            SrcAccessMask = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit
        };

        AttachmentDescription[] attachments = new[] {
            colorAttachment,
            depthAttachment,
            colorAttachmentResolve
        };

        fixed (AttachmentDescription* attachmentsPtr = attachments) {
            RenderPassCreateInfo renderPassInfo = new() {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint) attachments.Length,
                PAttachments = attachmentsPtr,
                SubpassCount = 1,
                PSubpasses = &subpass,
                DependencyCount = 1,
                PDependencies = &dependency,
            };

            if (this.Vk.CreateRenderPass(this.VkDevice, renderPassInfo, null, out this._renderPass) != Result.Success) {
                throw new Exception("Failed to create render pass!");
            }
        }
    }
    
    private unsafe void CreateFrameBuffers() {
        this._swapChainFrameBuffers = new Framebuffer[this._swapChainImageViews.Length];

        for (int i = 0; i < this._swapChainImageViews.Length; i++) {
            ImageView[] attachments = new[] {
                this._colorImageViews[i],
                this._depthImageViews[i],
                this._swapChainImageViews[i]
            };

            fixed (ImageView* attachmentsPtr = attachments) {
                FramebufferCreateInfo framebufferInfo = new() {
                    SType = StructureType.FramebufferCreateInfo,
                    RenderPass = this._renderPass,
                    AttachmentCount = (uint) attachments.Length,
                    PAttachments = attachmentsPtr,
                    Width = this._swapChainExtent.Width,
                    Height = this._swapChainExtent.Height,
                    Layers = 1
                };

                if (this.Vk.CreateFramebuffer(this.Device.GetVkDevice(), framebufferInfo, null, out this._swapChainFrameBuffers[i]) != Result.Success) {
                    throw new Exception("Failed to create framebuffer!");
                }
            }
        }
    }

    private unsafe void CreateColorResources() {
        Format colorFormat = this.SwapChainImageFormat;

        int imageCount = this._swapChainImageViews.Length;
        this._colorImages = new Image[imageCount];
        this._colorImageMemories = new DeviceMemory[imageCount];
        this._colorImageViews = new ImageView[imageCount];

        for (int i = 0; i < imageCount; i++) {
            ImageCreateInfo imageInfo = new() {
                SType = StructureType.ImageCreateInfo,
                ImageType = ImageType.Type2D,
                Extent = {
                    Width = this._swapChainExtent.Width,
                    Height = this._swapChainExtent.Height,
                    Depth = 1,
                },
                MipLevels = 1,
                ArrayLayers = 1,
                Format = colorFormat,
                Tiling = ImageTiling.Optimal,
                InitialLayout = ImageLayout.Undefined,
                Usage = ImageUsageFlags.TransientAttachmentBit | ImageUsageFlags.ColorAttachmentBit,
                Samples = this.Device.MsaaSamples,
                SharingMode = SharingMode.Exclusive,
                Flags = 0
            };

            fixed (Image* imagePtr = &this._colorImages[i]) {
                if (this.Vk.CreateImage(this.VkDevice, imageInfo, null, imagePtr) != Result.Success) {
                    throw new Exception("Failed to create color image!");
                }
            }

            this.Vk.GetImageMemoryRequirements(this.VkDevice, this._colorImages[i], out MemoryRequirements memRequirements);

            MemoryAllocateInfo allocInfo = new() {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = this.Device.FindMemoryType(memRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
            };

            fixed (DeviceMemory* imageMemoryPtr = &this._colorImageMemories[i]) {
                if (this.Vk.AllocateMemory(this.VkDevice, allocInfo, null, imageMemoryPtr) != Result.Success) {
                    throw new Exception("Failed to allocate color image memory!");
                }
            }

            this.Vk.BindImageMemory(this.VkDevice, this._colorImages[i], this._colorImageMemories[i], 0);
            
            ImageViewCreateInfo createInfo = new() {
                SType = StructureType.ImageViewCreateInfo,
                Image = this._colorImages[i],
                ViewType = ImageViewType.Type2D,
                Format = colorFormat,
                SubresourceRange = {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                }
            };

            if (this.Vk.CreateImageView(this.VkDevice, createInfo, null, out this._colorImageViews[i]) != Result.Success) {
                throw new Exception("Failed to create color image views!");
            }
        }
    }

    private unsafe void CreateDepthResources() {
        this.SwapChainDepthFormat = this.Device.FindDepthFormat();

        int imageCount = this._swapChainImageViews.Length;
        this._depthImages = new Image[imageCount];
        this._depthImageMemories = new DeviceMemory[imageCount];
        this._depthImageViews = new ImageView[imageCount];

        for (int i = 0; i < imageCount; i++) {
            ImageCreateInfo imageInfo = new() {
                SType = StructureType.ImageCreateInfo,
                ImageType = ImageType.Type2D,
                Extent = new() {
                    Width = this._swapChainExtent.Width,
                    Height = this._swapChainExtent.Height,
                    Depth = 1
                },
                MipLevels = 1,
                ArrayLayers = 1,
                Format = this.SwapChainDepthFormat,
                Tiling = ImageTiling.Optimal,
                InitialLayout = ImageLayout.Undefined,
                Usage = ImageUsageFlags.DepthStencilAttachmentBit,
                Samples = Device.MsaaSamples,
                SharingMode = SharingMode.Exclusive,
                Flags = 0
            };

            fixed (Image* imagePtr = &this._depthImages[i]) {
                if (this.Vk.CreateImage(this.VkDevice, imageInfo, null, imagePtr) != Result.Success) {
                    throw new Exception("Failed to create depth image!");
                }
            }

            this.Vk.GetImageMemoryRequirements(this.VkDevice, this._depthImages[i], out MemoryRequirements memoryRequirements);

            MemoryAllocateInfo allocInfo = new() {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
                MemoryTypeIndex = this.Device.FindMemoryType(memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit),
            };

            fixed (DeviceMemory* imageMemoryPtr = &this._depthImageMemories[i]) {
                if (this.Vk.AllocateMemory(this.VkDevice, allocInfo, null, imageMemoryPtr) != Result.Success) {
                    throw new Exception("Failed to allocate depth image memory!");
                }
            }

            this.Vk.BindImageMemory(this.VkDevice, this._depthImages[i], this._depthImageMemories[i], 0);

            ImageViewCreateInfo createInfo = new() {
                SType = StructureType.ImageViewCreateInfo,
                Image = this._depthImages[i],
                ViewType = ImageViewType.Type2D,
                Format = this.SwapChainDepthFormat,
                SubresourceRange = new() {
                    AspectMask = ImageAspectFlags.DepthBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                }
            };

            if (this.Vk.CreateImageView(this.VkDevice, createInfo, null, out this._depthImageViews[i]) != Result.Success) {
                throw new Exception("Failed to create depth image views!");
            }
        }
    }

    private unsafe void CreateImage(uint width, uint height, uint mipLevels, SampleCountFlags numSamples, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, ref Image image, ref DeviceMemory imageMemory) {
        ImageCreateInfo imageInfo = new() {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new() {
                Width = width,
                Height = height,
                Depth = 1
            },
            MipLevels = mipLevels,
            ArrayLayers = 1,
            Format = format,
            Tiling = tiling,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
            Samples = numSamples,
            SharingMode = SharingMode.Exclusive
        };

        fixed (Image* imagePtr = &image) {
            if (this.Vk.CreateImage(this.VkDevice, imageInfo, null, imagePtr) != Result.Success) {
                throw new Exception("Failed to create image!");
            }
        }

        this.Vk.GetImageMemoryRequirements(this.VkDevice, image, out MemoryRequirements memoryRequirements);

        MemoryAllocateInfo allocInfo = new() {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memoryRequirements.Size,
            MemoryTypeIndex = this.Device.FindMemoryType(memoryRequirements.MemoryTypeBits, properties)
        };

        fixed (DeviceMemory* imageMemoryPtr = &imageMemory) {
            if (this.Vk.AllocateMemory(this.VkDevice, allocInfo, null, imageMemoryPtr) != Result.Success) {
                throw new Exception("Failed to allocate image memory!");
            }
        }

        this.Vk.BindImageMemory(this.VkDevice, image, imageMemory, 0);
    }

    private unsafe void CreateSyncObjects() {
        this._imageAvailableSemaphores = new Semaphore[MaxFramesInFlight];
        this._renderFinishedSemaphores = new Semaphore[MaxFramesInFlight];
        this._inFlightFences = new Fence[MaxFramesInFlight];
        this._imagesInFlight = new Fence[this._swapChainImages.Length];

        SemaphoreCreateInfo semaphoreInfo = new() {
            SType = StructureType.SemaphoreCreateInfo
        };

        FenceCreateInfo fenceInfo = new() {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit
        };

        for (var i = 0; i < MaxFramesInFlight; i++) {
            if (this.Vk.CreateSemaphore(this.VkDevice, semaphoreInfo, null, out this._imageAvailableSemaphores[i]) != Result.Success ||
                this.Vk.CreateSemaphore(this.VkDevice, semaphoreInfo, null, out this._renderFinishedSemaphores[i]) != Result.Success ||
                this.Vk.CreateFence(this.VkDevice, fenceInfo, null, out this._inFlightFences[i]) != Result.Success) {
                throw new Exception("Failed to create synchronization objects for a frame!");
            }
        }
    }

    private SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats) {
        foreach (var availableFormat in availableFormats) {
            if (availableFormat.Format == Format.B8G8R8A8Srgb && availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr) {
                return availableFormat;
            }
        }

        return availableFormats[0];
    }

    private PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes) {
        if (this.UseFifo) return PresentModeKHR.FifoKhr;

        foreach (var availablePresentMode in availablePresentModes) {
            if (availablePresentMode == PresentModeKHR.MailboxKhr) {
                Logger.Info("Swapchain got present mode = Mailbox");
                return availablePresentMode;
            }
        }

        Logger.Info("Swapchain fallback present mode = FifoKhr");
        return PresentModeKHR.FifoKhr;
    }

    private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities) {
        if (capabilities.CurrentExtent.Width != uint.MaxValue) {
            return capabilities.CurrentExtent;
        }
        else {
            Extent2D framebufferSize = this._windowExtent;

            Extent2D actualExtent = new() {
                Width = framebufferSize.Width,
                Height = framebufferSize.Height
            };

            actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
            actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

            return actualExtent;
        }
    }
        
    protected override unsafe void Dispose(bool disposing) {
        if (disposing) {
            foreach (Framebuffer framebuffer in this._swapChainFrameBuffers) {
                this.Vk.DestroyFramebuffer(this.Device.GetVkDevice(), framebuffer, null);
            }

            foreach (ImageView imageView in this._swapChainImageViews) {
                this.Vk.DestroyImageView(this.Device.GetVkDevice(), imageView, null);
            }
            Array.Clear(this._swapChainImageViews);

            for (int i = 0; i < this._depthImages.Length; i++) {
                this.Vk.DestroyImageView(this.Device.GetVkDevice(), this._depthImageViews[i], null);
                this.Vk.DestroyImage(this.Device.GetVkDevice(), this._depthImages[i], null);
                this.Vk.FreeMemory(this.Device.GetVkDevice(), this._depthImageMemories[i], null);
            }

            for (int i = 0; i < this._colorImages.Length; i++) {
                this.Vk.DestroyImageView(this.Device.GetVkDevice(), this._colorImageViews[i], null);
                this.Vk.DestroyImage(this.Device.GetVkDevice(), this._colorImages[i], null);
                this.Vk.FreeMemory(this.Device.GetVkDevice(), this._colorImageMemories[i], null);
            }

            this.Vk.DestroyRenderPass(this.Device.GetVkDevice(), this._renderPass, null);

            for (int i = 0; i < MaxFramesInFlight; i++) {
                this.Vk.DestroySemaphore(this.Device.GetVkDevice(), this._renderFinishedSemaphores[i], null);
                this.Vk.DestroySemaphore(this.Device.GetVkDevice(), this._imageAvailableSemaphores[i], null);
                this.Vk.DestroyFence(this.Device.GetVkDevice(), this._inFlightFences[i], null);
            }

            this._khrSwapChain.DestroySwapchain(this.Device.GetVkDevice(), this._swapChain, null);
            this._swapChain = default;
        }
    }
}