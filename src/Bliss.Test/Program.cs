using Bliss.Test;
using Veldrid;

GameSettings settings = new GameSettings() {
    Title = "Bliss - [Test]",
    VSync = false,
    Backend = GraphicsBackend.OpenGL // TODO: REmove it just for the ssbo
};

using Game game = new Game(settings);
game.Run();