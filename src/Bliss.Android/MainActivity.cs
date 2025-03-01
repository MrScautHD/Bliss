using Bliss.Android.CSharp;
using Org.Libsdl.App;
using Veldrid;

namespace Bliss.Android;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : SDLActivity {
    
    private BlissSurfaceView _surfaceView;
    private Game _game;
    
    protected override void OnCreate(Bundle? savedInstanceState) {
        base.OnCreate(savedInstanceState);
        
        GraphicsBackend backend = GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan) ? GraphicsBackend.Vulkan : GraphicsBackend.OpenGLES;
        
        GraphicsDeviceOptions options = new GraphicsDeviceOptions() {
            Debug = false,
            HasMainSwapchain = true,
            SwapchainDepthFormat = PixelFormat.D32FloatS8UInt, // R16_UNorm
            SyncToVerticalBlank = false,
            ResourceBindingModel = ResourceBindingModel.Improved,
            PreferDepthRangeZeroToOne = true,
            PreferStandardClipSpaceYDirection = true,
            SwapchainSrgbFormat = false
        };

        this._surfaceView = new BlissSurfaceView(this, backend, options);
        this._game = new Game(this._surfaceView);
        this._game.Run();

        // Set our view from the "main" layout resource.
        this.SetContentView(Resource.Layout.activity_main);
    }
    
    protected override string[] GetLibraries() => ["SDL3"];

    protected override void OnPause() {
        base.OnPause();
        this._surfaceView.OnPause();
    }

    protected override void OnResume() {
        base.OnResume();
        this._surfaceView.OnResume();
    }
}