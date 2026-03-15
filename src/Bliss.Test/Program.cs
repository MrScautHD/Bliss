using Bliss.CSharp.Logging;
using Bliss.Test;

try {
    GameSettings settings = new GameSettings() {
        Title = "Bliss - [Test]",
        VSync = false
    };

    using Game game = new Game(settings);
    game.Run();
}
catch (Exception ex) {
    Logger.Error(ex.ToString());
    Environment.ExitCode = 1;
}