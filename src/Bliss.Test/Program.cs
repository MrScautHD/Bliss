using Bliss.Test;

GameSettings settings = new GameSettings() {
    Title = "Bliss - [Test]",
    VSync = true
};

using Game game = new Game(settings);
game.Run();