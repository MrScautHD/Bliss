using System.Diagnostics;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Veldrid;

namespace Bliss.Android.CSharp;

public class BlissSurfaceView : SurfaceView, ISurfaceHolderCallback {
    
    public GraphicsDevice GraphicsDevice { get; private set; }
    public Swapchain MainSwapchain { get; private set; }
    public GraphicsBackend Backend { get; private set; }
    public GraphicsDeviceOptions DeviceOptions { get; private set; }
    
    private bool _surfaceDestroyed;
    private bool _paused;
    private bool _enabled;
    private bool _needsResize;
    private bool _surfaceCreated;
    
    public event Action Rendering;
    public event Action DeviceCreated;
    public event Action DeviceDisposed;
    public event Action Resized;
    
    public BlissSurfaceView(Context context, GraphicsBackend backend, GraphicsDeviceOptions deviceOptions) : base(context) {
        if (!(backend == GraphicsBackend.Vulkan || backend == GraphicsBackend.OpenGLES)) {
            throw new NotSupportedException($"{backend} is not supported on Android.");
        }

        this.Backend = backend;
        this.DeviceOptions = deviceOptions;
        this.Holder.AddCallback(this);
    }

    public void SurfaceCreated(ISurfaceHolder holder) {
        bool deviceCreated = false;
        if (this.Backend == GraphicsBackend.Vulkan) {
            if (this.GraphicsDevice == null!) {
                this.GraphicsDevice = GraphicsDevice.CreateVulkan(this.DeviceOptions);
                deviceCreated = true;
            }

            Debug.Assert(this.MainSwapchain == null);
            SwapchainSource swapchainSource = SwapchainSource.CreateAndroidSurface(holder.Surface.Handle, JNIEnv.Handle);
            SwapchainDescription swapchainDescription= new SwapchainDescription(swapchainSource, (uint) this.Width, (uint) this.Height, this.DeviceOptions.SwapchainDepthFormat, this.DeviceOptions.SyncToVerticalBlank);
            this.MainSwapchain = this.GraphicsDevice.ResourceFactory.CreateSwapchain(swapchainDescription);
        }
        else {
            Debug.Assert(this.GraphicsDevice == null && this.MainSwapchain == null);
            SwapchainSource ss = SwapchainSource.CreateAndroidSurface(holder.Surface.Handle, JNIEnv.Handle);
            SwapchainDescription sd = new SwapchainDescription(ss, (uint) this.Width, (uint) this.Height, this.DeviceOptions.SwapchainDepthFormat, this.DeviceOptions.SyncToVerticalBlank);
            this.GraphicsDevice = GraphicsDevice.CreateOpenGLES(this.DeviceOptions, sd);
            this.MainSwapchain = this.GraphicsDevice.MainSwapchain;
            deviceCreated = true;
        }

        if (deviceCreated) {
            DeviceCreated?.Invoke();
        }

        this._surfaceCreated = true;
    }
    
    public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height) {
        this._needsResize = true;
    }

    public void SurfaceDestroyed(ISurfaceHolder holder) {
        this._surfaceDestroyed = true;
    }

    public void RunContinuousRenderLoop() {
        Task.Factory.StartNew(() => this.RenderLoop(), TaskCreationOptions.LongRunning);
    }

    public void OnPause() {
        this._paused = true;
    }

    public void OnResume() {
        this._paused = false;
    }
    
    private void RenderLoop() {
        this._enabled = true;
        while (this._enabled) {
            try {
                if (this._paused || !this._surfaceCreated) {
                    continue;
                }

                if (this._surfaceDestroyed) {
                    HandleSurfaceDestroyed();
                    continue;
                }

                if (this._needsResize) {
                    this._needsResize = false;
                    this.MainSwapchain.Resize((uint) this.Width, (uint) this.Height);
                    Resized?.Invoke();
                }

                if (this.GraphicsDevice != null) {
                    Rendering?.Invoke();
                }
            }
            catch (Exception e) {
                Debug.WriteLine("Encountered an error while rendering: " + e);
                throw;
            }
        }
    }
    
    private void HandleSurfaceDestroyed() {
        if (this.Backend == GraphicsBackend.Vulkan) {
            this.MainSwapchain.Dispose();
            this.MainSwapchain = null;
        }
        else {
            GraphicsDevice.Dispose();
            GraphicsDevice = null;
            MainSwapchain = null;
            DeviceDisposed?.Invoke();
        }
    }
}