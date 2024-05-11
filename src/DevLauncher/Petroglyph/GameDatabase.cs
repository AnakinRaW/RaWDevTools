using System;
using System.Collections.Generic;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph;

public class GameDatabase(GameRepository gameRepository, IServiceProvider serviceProvider)
{
    public GameRepository GameRepository { get; } = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));

    public required GameConstants GameConstants { get; init; }

    public required IList<GameObject> GameObjects { get; init; }
}