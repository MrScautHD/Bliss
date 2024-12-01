/*
 * Copyright (c) 2024 Elias Springer (@MrScautHD)
 * License-Identifier: MIT License
 * 
 * For full license details, see:
 * https://github.com/MrScautHD/Bliss/blob/main/LICENSE
 */

using Bliss.Test;

GameSettings settings = new GameSettings() {
    Title = "Bliss - [Test]"
};

using Game game = new Game(settings);
game.Run();