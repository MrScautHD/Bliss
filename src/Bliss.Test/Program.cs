using Bliss.CSharp.Logging;
using Bliss.Test;
using Veldrid;

//try {
    GameSettings settings = new GameSettings() {
        Title = "Bliss - [Test]",
        //Backend = GraphicsBackend.OpenGL,
        VSync = false
    };

    using Game game = new Game(settings);
    game.Run();
//}
//catch (Exception ex) {
//    Logger.Error(ex.ToString());
//    Environment.ExitCode = 1;
//}