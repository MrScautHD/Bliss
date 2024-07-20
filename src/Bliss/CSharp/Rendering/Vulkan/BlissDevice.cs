using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Bliss.CSharp.Logging;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Bliss.CSharp.Rendering.Vulkan;

public class BlissDevice : Disposable {
    
    public readonly Vk Vk;
    public string DeviceName { get; private set; }
    public SampleCountFlags MsaaSamples { get; private set; }
    
    private Device _device;
    private readonly IView _window;
    
    private Instance _instance;
    
    private KhrSurface _khrSurface;
    private SurfaceKHR _surface;
    
    private PhysicalDevice _physicalDevice;
    private readonly string[] _deviceExtensions;
    
    private bool _enableValidationLayers;
    private string[] _validationLayers;

    private uint _graphicsFamilyIndex;
    
    private Queue _graphicsQueue;
    private Queue _presentQueue;

    private CommandPool _commandPool;
    
    private ExtDebugUtils _debugUtils;
    private DebugUtilsMessengerEXT _debugMessenger;
    
    /// <summary>
    /// Constructor for creating a BlissDevice object.
    /// </summary>
    /// <param name="vk">The Vulkan instance.</param>
    /// <param name="window">The window view.</param>
    public BlissDevice(Vk vk, IView window) {
        this.Vk = vk;
        this.DeviceName = "Unknown";
        this._window = window;
        this._enableValidationLayers = true;
        
        this._validationLayers = new [] {
            "VK_LAYER_KHRONOS_validation"
        };
        
        this._deviceExtensions = new[] {
            KhrSwapchain.ExtensionName,
            KhrSynchronization2.ExtensionName
        };
        
        this.CreateInstance();
        this.SetupDebugMessenger();
        this.CreateSurface();
        this.PickPhysicalDevice();
        this.CreateLogicalDevice();
        this.CreateCommandPool();
    }

    /// <summary>
    /// Creates a Vulkan instance.
    /// </summary>
    private unsafe void CreateInstance() {
        if (this._enableValidationLayers && !this.CheckValidationLayerSupport()) {
            throw new Exception("Validation layers requested, but not available!");
        }

        ApplicationInfo appInfo = new() {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*) Marshal.StringToHGlobalAnsi("Bliss"),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*) Marshal.StringToHGlobalAnsi("Bliss"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version12
        };

        InstanceCreateInfo createInfo = new() {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        string[] extensions = this.GetRequiredExtensions();
        createInfo.EnabledExtensionCount = (uint) extensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**) SilkMarshal.StringArrayToPtr(extensions);

        if (this._enableValidationLayers) {
            createInfo.EnabledLayerCount = (uint) this._validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**) SilkMarshal.StringArrayToPtr(this._validationLayers);

            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
            this.PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.PNext = &debugCreateInfo;
        }
        else {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        if (this.Vk.CreateInstance(createInfo, null, out this._instance) != Result.Success) {
            throw new Exception("Failed to create instance!");
        }

        Marshal.FreeHGlobal((nint) appInfo.PApplicationName);
        Marshal.FreeHGlobal((nint) appInfo.PEngineName);
        SilkMarshal.Free((nint) createInfo.PpEnabledExtensionNames);

        if (this._enableValidationLayers) {
            SilkMarshal.Free((nint) createInfo.PpEnabledLayerNames);
        }
    }

    /// <summary>
    /// Sets up the debug messenger for Vulkan validation layers, if enabled.
    /// </summary>
    private unsafe void SetupDebugMessenger() {
        if (!this._enableValidationLayers || !this.Vk.TryGetInstanceExtension(this._instance, out this._debugUtils)) {
            return;
        }

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        this.PopulateDebugMessengerCreateInfo(ref createInfo);

        if (this._debugUtils!.CreateDebugUtilsMessenger(this._instance, in createInfo, null, out this._debugMessenger) != Result.Success) {
            throw new Exception("Failed to set up debug messenger!");
        }
    }

    /// <summary>
    /// Creates a Vulkan surface.
    /// </summary>
    private unsafe void CreateSurface() {
        if (!this.Vk.TryGetInstanceExtension(this._instance, out this._khrSurface)) {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        if (this._window.VkSurface == null) {
            throw new ApplicationException("VkSurface is null and shouldn't be!");
        }

        this._surface = this._window.VkSurface.Create<AllocationCallbacks>(this._instance.ToHandle(), null).ToSurface();
    }

    /// <summary>
    /// Method for selecting a physical device that supports Vulkan.
    /// </summary>
    private unsafe void PickPhysicalDevice() {
        uint devicedCount = 0;
        this.Vk.EnumeratePhysicalDevices(this._instance, ref devicedCount, null);

        if (devicedCount == 0) {
            throw new Exception("Failed to find GPUs with Vulkan support!");
        }

        PhysicalDevice[] devices = new PhysicalDevice[devicedCount];
        fixed (PhysicalDevice* devicesPtr = devices) {
            this.Vk.EnumeratePhysicalDevices(this._instance, ref devicedCount, devicesPtr);
        }

        foreach (var device in devices) {
            if (this.IsDeviceSuitable(device)) {
                this._physicalDevice = device;
                this.MsaaSamples = this.GetMaxUsableSampleCount();
                break;
            }
        }

        if (this._physicalDevice.Handle == 0) {
            throw new Exception("failed to find a suitable GPU!");
        }

        this.Vk.GetPhysicalDeviceProperties(this._physicalDevice, out PhysicalDeviceProperties properties);
        this.DeviceName = Encoding.UTF8.GetString(properties.DeviceName, 50).Trim();
        Logger.Info($"Using device: {this.DeviceName}.");
    }

    /// <summary>
    /// Creates the logical device for Vulkan rendering.
    /// </summary>
    private unsafe void CreateLogicalDevice() {
        QueueFamilyIndices indices = this.FindQueueFamilies(this._physicalDevice);

        uint[] uniqueQueueFamilies = new[] {
            indices.GraphicsFamily!.Value,
            indices.PresentFamily!.Value
        }.Distinct().ToArray();
        
        this._graphicsFamilyIndex = indices.GraphicsFamily.Value;

        using GlobalMemory memory = GlobalMemory.Allocate(uniqueQueueFamilies.Length * Marshal.SizeOf<DeviceQueueCreateInfo>());
        DeviceQueueCreateInfo* queueCreateInfos = (DeviceQueueCreateInfo*) Unsafe.AsPointer(ref memory.GetPinnableReference());

        float queuePriority = 1.0f;
        
        for (int i = 0; i < uniqueQueueFamilies.Length; i++) {
            queueCreateInfos[i] = new DeviceQueueCreateInfo() {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1
            };

            queueCreateInfos[i].PQueuePriorities = &queuePriority;
        }

        PhysicalDeviceFeatures deviceFeatures = new() {
            SamplerAnisotropy = true
        };
        
        PhysicalDeviceSynchronization2FeaturesKHR sync2Features = new() {
            SType = StructureType.PhysicalDeviceSynchronization2FeaturesKhr,
            Synchronization2 = Vk.True
        };

        PhysicalDeviceFeatures2 deviceFeatures2 = new() {
            SType = StructureType.PhysicalDeviceFeatures2,
            PNext = &sync2Features
        };

        this.Vk.GetPhysicalDeviceFeatures2(this._physicalDevice, &deviceFeatures2);

        DeviceCreateInfo createInfo = new() {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint) uniqueQueueFamilies.Length,
            PQueueCreateInfos = queueCreateInfos,
            
            PEnabledFeatures = &deviceFeatures,
            PNext = &sync2Features,
            
            EnabledExtensionCount = (uint) this._deviceExtensions.Length,
            PpEnabledExtensionNames = (byte**) SilkMarshal.StringArrayToPtr(this._deviceExtensions)
        };

        if (this._enableValidationLayers) {
            createInfo.EnabledLayerCount = (uint) this._validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**) SilkMarshal.StringArrayToPtr(this._validationLayers);
        }
        else {
            createInfo.EnabledLayerCount = 0;
        }

        if (this.Vk.CreateDevice(this._physicalDevice, in createInfo, null, out this._device) != Result.Success) {
            throw new Exception("Failed to create logical device!");
        }

        this.Vk.GetDeviceQueue(this._device, indices.GraphicsFamily!.Value, 0, out this._graphicsQueue);
        this.Vk.GetDeviceQueue(this._device, indices.PresentFamily!.Value, 0, out this._presentQueue);

        if (this._enableValidationLayers) {
            SilkMarshal.Free((nint) createInfo.PpEnabledLayerNames);
        }

        SilkMarshal.Free((nint) createInfo.PpEnabledExtensionNames);
    }

    /// <summary>
    /// Creates a command pool for the BlissDevice object.
    /// </summary>
    private unsafe void CreateCommandPool() {
        QueueFamilyIndices queueFamilyIndices = this.FindQueueFamilies(this._physicalDevice);

        CommandPoolCreateInfo poolInfo = new() {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value,
            Flags = CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit
        };

        if (this.Vk.CreateCommandPool(this._device, poolInfo, null, out this._commandPool) != Result.Success) {
            throw new Exception("Failed to create command pool!");
        }
    }

    /// <summary>
    /// Populates the DebugUtilsMessengerCreateInfoEXT structure with the necessary information for creating a debug messenger.
    /// </summary>
    /// <param name="createInfo">The DebugUtilsMessengerCreateInfoEXT structure to be populated.</param>
    private unsafe void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo) {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT) this.DebugCallback;
    }

    /// <summary>
    /// Callback used for debugging messages.
    /// </summary>
    /// <param name="messageSeverity">The severity level of the message.</param>
    /// <param name="messageTypes">The types of the message.</param>
    /// <param name="pCallbackData">Pointer to the debug message data.</param>
    /// <param name="pUserData">Pointer to user-defined data.</param>
    /// <returns>The result of the debug callback.</returns>
    private unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData) {
        if (messageSeverity == DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt) return Vk.False;

        string? msg = Marshal.PtrToStringAnsi((nint) pCallbackData->PMessage);
        Debug.WriteLine($"{messageSeverity} | validation layer: {msg}");

        return Vk.False;
    }

    /// <summary>
    /// Creates a Vulkan buffer with specified size, usage, and memory properties.
    /// </summary>
    /// <param name="size">The size in bytes of the buffer.</param>
    /// <param name="usage">The intended usage of the buffer.</param>
    /// <param name="properties">The required memory properties for the buffer.</param>
    /// <param name="buffer">Reference to the created Vulkan buffer.</param>
    /// <param name="bufferMemory">Reference to the allocated Vulkan device memory for the buffer.</param>
    public unsafe void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, ref Buffer buffer, ref DeviceMemory bufferMemory) {
        BufferCreateInfo bufferInfo = new() {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
        };

        fixed (Buffer* bufferPtr = &buffer) {
            if (this.Vk.CreateBuffer(this._device, bufferInfo, null, bufferPtr) != Result.Success) {
                throw new Exception("Failed to create vertex buffer!");
            }
        }

        this.Vk.GetBufferMemoryRequirements(this._device, buffer, out MemoryRequirements memRequirements);

        MemoryAllocateInfo allocateInfo = new() {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = this.FindMemoryType(memRequirements.MemoryTypeBits, properties),
        };

        fixed (DeviceMemory* bufferMemoryPtr = &bufferMemory) {
            if (this.Vk.AllocateMemory(this._device, allocateInfo, null, bufferMemoryPtr) != Result.Success) {
                throw new Exception("Failed to allocate vertex buffer memory!");
            }
        }

        this.Vk.BindBufferMemory(this._device, buffer, bufferMemory, 0);
    }

    /// <summary>
    /// Copies the contents of one buffer to another buffer.
    /// </summary>
    /// <param name="srcBuffer">The source buffer.</param>
    /// <param name="dstBuffer">The destination buffer.</param>
    /// <param name="size">The size, in bytes, of the data to be copied.</param>
    public void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size) {
        CommandBuffer commandBuffer = this.BeginSingleTimeCommands();

        BufferCopy copyRegion = new() {
            Size = size,
        };

        this.Vk.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, copyRegion);

        this.EndSingleTimeCommands(commandBuffer);
    }

    /// <summary>
    /// Begins the recording of a single time command buffer.
    /// </summary>
    /// <returns>The command buffer that is allocated and started.</returns>
    private CommandBuffer BeginSingleTimeCommands() {
        CommandBufferAllocateInfo allocateInfo = new() {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = this._commandPool,
            CommandBufferCount = 1,
        };

        this.Vk.AllocateCommandBuffers(this._device, allocateInfo, out CommandBuffer commandBuffer);

        CommandBufferBeginInfo beginInfo = new() {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        this.Vk.BeginCommandBuffer(commandBuffer, beginInfo);

        return commandBuffer;
    }

    /// <summary>
    /// Ends a single time command buffer execution.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to end.</param>
    private unsafe void EndSingleTimeCommands(CommandBuffer commandBuffer) {
        this.Vk.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new() {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        this.Vk.QueueSubmit(this._graphicsQueue, 1, submitInfo, default);
        this.Vk.QueueWaitIdle(this._graphicsQueue);

        this.Vk.FreeCommandBuffers(this._device, this._commandPool, 1, commandBuffer);
    }

    /// <summary>
    /// Queries the swap chain support of the physical device.
    /// </summary>
    /// <param name="physicalDevice">The physical device.</param>
    /// <returns>The swap chain support details.</returns>
    private unsafe SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice physicalDevice) {
        SwapChainSupportDetails details = new();
        
        this._khrSurface.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, this._surface, out details.Capabilities);

        uint formatCount = 0;
        this._khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, this._surface, ref formatCount, null);

        if (formatCount != 0) {
            details.Formats = new SurfaceFormatKHR[formatCount];
            
            fixed (SurfaceFormatKHR* formatsPtr = details.Formats) {
                this._khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, this._surface, ref formatCount, formatsPtr);
            }
        }
        else {
            details.Formats = [];
        }

        uint presentModeCount = 0;
        this._khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, this._surface, ref presentModeCount, null);

        if (presentModeCount != 0) {
            details.PresentModes = new PresentModeKHR[presentModeCount];
            
            fixed (PresentModeKHR* formatsPtr = details.PresentModes) {
                this._khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, this._surface, ref presentModeCount, formatsPtr);
            }
        }
        else {
            details.PresentModes = [];
        }

        return details;
    }

    /// <summary>
    /// Check if a given physical device is suitable for the application.
    /// </summary>
    /// <param name="device">The physical device to check.</param>
    /// <returns><c>true</c> if the device is suitable; otherwise, <c>false</c>.</returns>
    private unsafe bool IsDeviceSuitable(PhysicalDevice device) {
        QueueFamilyIndices indices = this.FindQueueFamilies(device);

        bool extensionsSupported = this.CheckDeviceExtensionsSupport(device);
        bool swapChainAdequate = false;
        
        if (extensionsSupported) {
            SwapChainSupportDetails swapChainSupport = this.QuerySwapChainSupport(device);
            swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
        }

        this.Vk.GetPhysicalDeviceFeatures(device, out PhysicalDeviceFeatures supportedFeatures);
        
        PhysicalDeviceSynchronization2FeaturesKHR sync2Features = new() {
            SType = StructureType.PhysicalDeviceSynchronization2FeaturesKhr,
            Synchronization2 = Vk.True
        };

        PhysicalDeviceFeatures2 deviceFeatures2 = new() {
            SType = StructureType.PhysicalDeviceFeatures2,
            PNext = &sync2Features
        };

        this.Vk.GetPhysicalDeviceFeatures2(device, &deviceFeatures2);

        return indices.IsComplete() && extensionsSupported && swapChainAdequate && supportedFeatures.SamplerAnisotropy && sync2Features.Synchronization2;
    }

    /// <summary>
    /// Checks if the specified device supports the required device extensions.
    /// </summary>
    /// <param name="device">The physical device to check.</param>
    /// <returns>True if the device supports all the required device extensions, false otherwise.</returns>
    private unsafe bool CheckDeviceExtensionsSupport(PhysicalDevice device) {
        uint extensionsCount = 0;
        this.Vk.EnumerateDeviceExtensionProperties(device, (byte*) null, ref extensionsCount, null);

        ExtensionProperties[] availableExtensions = new ExtensionProperties[extensionsCount];
        
        fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions) {
            this.Vk.EnumerateDeviceExtensionProperties(device, (byte*) null, ref extensionsCount, availableExtensionsPtr);
        }

        HashSet<string?> availableExtensionNames = availableExtensions.Select(extension => {
            if (extension.ExtensionName != null) {
                return Marshal.PtrToStringAnsi((nint) extension.ExtensionName);
            }
            
            throw new InvalidOperationException("Could not convert pointer to string.");
        }).ToHashSet();

        return this._deviceExtensions.All(availableExtensionNames.Contains);
    }

    /// <summary>
    /// Finds the queue families available on the given physical device.
    /// </summary>
    /// <param name="device">The physical device to query.</param>
    /// <returns>The queue family indices containing the graphics and presentation families.</returns>
    private unsafe QueueFamilyIndices FindQueueFamilies(PhysicalDevice device) {
        QueueFamilyIndices indices = new();

        uint queueFamilityCount = 0;
        this.Vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

        QueueFamilyProperties[] queueFamilies = new QueueFamilyProperties[queueFamilityCount];
        
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies) {
            this.Vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
        }

        uint i = 0;
        foreach (var queueFamily in queueFamilies) {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit)) {
                indices.GraphicsFamily = i;
            }

            this._khrSurface.GetPhysicalDeviceSurfaceSupport(device, i, this._surface, out Bool32 presentSupport);

            if (presentSupport) {
                indices.PresentFamily = i;
            }

            if (indices.IsComplete()) {
                break;
            }

            i++;
        }

        return indices;
    }

    /// <summary>
    /// Retrieves the required Vulkan extensions based on the window view.
    /// </summary>
    /// <returns>An array of strings representing the required Vulkan extensions.</returns>
    private unsafe string[] GetRequiredExtensions() {
        byte** glfwExtensions = this._window.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        string[] extensions = SilkMarshal.PtrToStringArray((nint) glfwExtensions, (int) glfwExtensionCount);

        if (this._enableValidationLayers) {
            return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
        }

        return extensions;
    }

    /// <summary>
    /// Checks if the required validation layers are supported by the Vulkan instance.
    /// </summary>
    /// <returns>Returns true if all validation layers are supported, otherwise false.</returns>
    private unsafe bool CheckValidationLayerSupport() {
        uint layerCount = 0;
        this.Vk.EnumerateInstanceLayerProperties(ref layerCount, null);
        
        LayerProperties[] availableLayers = new LayerProperties[layerCount];
        
        fixed (LayerProperties* availableLayersPtr = availableLayers) {
            this.Vk.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
        }

        HashSet<string?> availableLayerNames = availableLayers.Select(layer => {
            if (layer.LayerName != null) {
                return Marshal.PtrToStringAnsi((nint) layer.LayerName);
            }

            throw new InvalidOperationException("Could not convert pointer to string.");
        }).ToHashSet();

        return this._validationLayers.All(availableLayerNames.Contains);
    }

    /// <summary>
    /// Gets the maximum usable sample count for the physical device.
    /// </summary>
    /// <returns>The maximum usable sample count.</returns>
    private SampleCountFlags GetMaxUsableSampleCount() {
        this.Vk.GetPhysicalDeviceProperties(this._physicalDevice, out var physicalDeviceProperties);
        
        SampleCountFlags counts = physicalDeviceProperties.Limits.FramebufferColorSampleCounts & physicalDeviceProperties.Limits.FramebufferDepthSampleCounts;

        return counts switch {
            var count when (count & SampleCountFlags.Count64Bit) != 0 => SampleCountFlags.Count64Bit,
            var count when (count & SampleCountFlags.Count32Bit) != 0 => SampleCountFlags.Count32Bit,
            var count when (count & SampleCountFlags.Count16Bit) != 0 => SampleCountFlags.Count16Bit,
            var count when (count & SampleCountFlags.Count8Bit) != 0 => SampleCountFlags.Count8Bit,
            var count when (count & SampleCountFlags.Count4Bit) != 0 => SampleCountFlags.Count4Bit,
            var count when (count & SampleCountFlags.Count2Bit) != 0 => SampleCountFlags.Count2Bit,
            _ => SampleCountFlags.Count1Bit
        };
    }

    /// <summary>
    /// Finds a supported format for the given candidates, tiling, and features.
    /// </summary>
    /// <param name="candidates">The list of format candidates.</param>
    /// <param name="tiling">The tiling mode.</param>
    /// <param name="features">The required format features.</param>
    /// <returns>The supported format.</returns>
    private Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features) {
        foreach (var format in candidates) {
            this.Vk.GetPhysicalDeviceFormatProperties(this._physicalDevice, format, out FormatProperties props);

            if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features) {
                return format;
            }
            else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features) {
                return format;
            }
        }

        throw new Exception("Failed to find supported format!");
    }

    /// <summary>
    /// Finds a supported format for the given candidates, tiling, and features.
    /// </summary>
    public Format FindDepthFormat() {
        return this.FindSupportedFormat(new[] {
            Format.D32Sfloat,
            Format.D32SfloatS8Uint,
            Format.D24UnormS8Uint
        }, ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
    }

    /// <summary>
    /// Finds the memory type that meets the specified requirements.
    /// </summary>
    /// <param name="typeFilter">The memory type filter.</param>
    /// <param name="properties">The desired memory properties.</param>
    /// <returns>The index of the matching memory type.</returns>
    public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties) {
        this.Vk.GetPhysicalDeviceMemoryProperties(this._physicalDevice, out PhysicalDeviceMemoryProperties memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++) {
            if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties) {
                return (uint) i;
            }
        }

        throw new Exception("Failed to find suitable memory type!");
    }

    /// <summary>
    /// Retrieves the Vulkan device associated with the BlissDevice.
    /// </summary>
    /// <returns>The Vulkan device.</returns>
    public Device GetVkDevice() {
        return this._device;
    }

    /// <summary>
    /// Returns the Vulkan instance associated with the BlissDevice object.
    /// </summary>
    /// <returns>The Vulkan instance.</returns
    public Instance GetInstance() {
        return this._instance;
    }

    /// <summary>
    /// Gets the Vulkan surface associated with the current BlissDevice object.
    /// </summary>
    /// <returns>The Vulkan surface.</returns>
    public SurfaceKHR GetSurface() {
        return this._surface;
    }

    /// <summary>
    /// Retrieves the graphics queue associated with the BlissDevice object.
    /// </summary>
    /// <returns>The graphics queue.</returns>
    public Queue GetGraphicsQueue() {
        return this._graphicsQueue;
    }

    /// <summary>
    /// Retrieves the present queue from the BlissDevice object.
    /// </summary>
    /// <returns>The present queue.</returns>
    public Queue GetPresentQueue() {
        return this._presentQueue;
    }

    /// <summary>
    /// Retrieves the command pool associated with the BlissDevice object.
    /// </summary>
    /// <returns>The command pool.</returns>
    public CommandPool GetCommandPool() {
        return this._commandPool;
    }

    /// <summary>
    /// Retrieves the physical device properties.
    /// </summary>
    /// <returns>The physical device properties.</returns>
    public PhysicalDeviceProperties GetProperties() {
        this.Vk.GetPhysicalDeviceProperties(this._physicalDevice, out PhysicalDeviceProperties properties);
        return properties;
    }
    /// <summary>>
    /// Queries the swap chain support of the physical device.
    /// </summary>
    /// <returns>The swap chain support details.</returns>
    public SwapChainSupportDetails QuerySwapChainSupport() {
        return this.QuerySwapChainSupport(this._physicalDevice);
    }

    /// <summary>
    /// Finds the queue families available on the given physical device.
    /// </summary>
    /// <returns>The queue family indices containing the graphics and presentation families.</returns>
    public QueueFamilyIndices FindQueueFamilies() {
        return this.FindQueueFamilies(this._physicalDevice);
    }

    /// <summary>
    /// Provides information about the swap chain support of a physical device.
    /// </summary>
    public struct SwapChainSupportDetails {
        public SurfaceCapabilitiesKHR Capabilities;
        public SurfaceFormatKHR[] Formats;
        public PresentModeKHR[] PresentModes;
    }

    /// <summary>
    /// Represents the indices of the queue families on a physical device.
    /// </summary>
    public struct QueueFamilyIndices {
        
        public uint? GraphicsFamily;
        public uint? PresentFamily;

        /// <summary>
        /// Checks if the device is suitable for use in the application based on various criteria such as queue family support, device extensions support, swap chain support, and supported features.
        /// </summary>
        /// <returns>True if the device is suitable, otherwise false.</returns>
        public bool IsComplete() {
            return this.GraphicsFamily.HasValue && this.PresentFamily.HasValue;
        }
    }
    
    protected override unsafe void Dispose(bool disposing) {
        if (disposing) {
            this.Vk.DestroyCommandPool(this._device, this._commandPool, null);
            this.Vk.DestroyDevice(this._device, null);
        }
    }
}