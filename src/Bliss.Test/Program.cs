using Bliss.Test;
using Veldrith;

//try {
    GameSettings settings = new GameSettings() {
        Title = "Bliss - [Test]",
        Backend = GraphicsBackend.Direct3D12,
        VSync = false
    };

    using Game game = new Game(settings);
    game.Run();
//}
//catch (Exception ex) {
//    Logger.Error(ex.ToString());
//    Environment.ExitCode = 1;
//}