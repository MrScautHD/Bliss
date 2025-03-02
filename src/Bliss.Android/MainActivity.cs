using Bliss.Test;
using Org.Libsdl.App;

namespace Bliss.Android;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : SDLActivity {
    
    // TODO: Make it work... If someone could help, pls do a PR :) (Make SDL3 compatible, Add Touch support...)
    // Building it:
    // 1. Connect your Phone with USB
    // 2. cd src
    // 3. cd Bliss.Android
    // 4. dotnet run
    // 5. Bliss.Test is "<OutputType>Exe</OutputType>" set it to "<OutputType>Library</OutputType>" for testing it! (If you not doing that it will crash.)
    
    protected override void OnCreate(Bundle? savedInstanceState) {
        base.OnCreate(savedInstanceState);

        GameSettings settings = new GameSettings() {
            Title = "Bliss - [Test]"
        };

        using Game game = new Game(settings);
        game.Run();
        
        // Set our view from the "main" layout resource.
        this.SetContentView(Resource.Layout.activity_main);
    }
    
    protected override string[] GetLibraries() => ["SDL3"];
}