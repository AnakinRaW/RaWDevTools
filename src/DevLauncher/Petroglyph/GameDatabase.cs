using System;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph;

public class GameDatabase(GameRepository gameRepository, IServiceProvider serviceProvider)
{
    public GameRepository GameRepository { get; } = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));

    public required GameConstants GameConstants { get; init; }
}