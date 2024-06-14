using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;

namespace RepublicAtWar.DevLauncher.Services;

public readonly struct GameFinderResult(IMod mod, IGame fallbackGame)
{
    public IMod Mod { get; } = mod;

    public IGame FallbackGame { get; } = fallbackGame;
}