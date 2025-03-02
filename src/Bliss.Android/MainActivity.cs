using Android.Runtime;
using Bliss.CSharp.Windowing;
using Bliss.Test;
using Org.Libsdl.App;

namespace Bliss.Android;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : SDLActivity {

    private Game _game;
    
    // TODO: Make it work... If someone could help, pls do a PR :) (Make SDL3 compatible, Add Touch support...)
    // TODO: Make Resource loading.
    // Building it:
    // 1. Connect your Phone with USB
    // 2. cd src
    // 3. cd Bliss.Android
    // 4. dotnet run
    // 5. Bliss.Test is "<OutputType>Exe</OutputType>" set it to "<OutputType>Library</OutputType>" for testing it! (If you not doing that it will crash.)
    
    protected override string[] GetLibraries() => ["SDL3"];
    
    protected override void Main() {
        Sdl3Window.AndroidSurface = () => MSurface?.NativeSurface?.Handle ?? nint.Zero;
        Sdl3Window.AndroidJNIEnv = () => JNIEnv.Handle;
        
        GameSettings settings = new GameSettings() {
            Title = "Bliss - [Test]"
        };
        
        this._game = new Game(settings);
        this._game.Run();
    }

    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);

        if (disposing) {
            // Dispose game here.
        }
    }
}