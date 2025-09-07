using Bliss.Test;

GameSettings settings = new GameSettings() {
    Title = "Bliss - [Test]",
    VSync = false
};

using Game game = new Game(settings);
game.Run();