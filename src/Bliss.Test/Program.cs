using Bliss.CSharp.Logging;
using Bliss.CSharp.Windowing;
using Bliss.Test;
using Veldrith;

//try {
    GameSettings settings = new GameSettings() {
        Title = "Bliss - [Test]",
        Backend = Window.GetPlatformDefaultBackend(),
        VSync = false
    };

    using Game game = new Game(settings);
    game.Run();
//}
//catch (Exception ex) {
//    Logger.Error(ex.ToString());
//    Environment.ExitCode = 1;
//}