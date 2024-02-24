using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PetroGlyph.Games.EawFoc.Clients;
using PetroGlyph.Games.EawFoc.Clients.Arguments;
using PetroGlyph.Games.EawFoc.Clients.Steam;
using PetroGlyph.Games.EawFoc.Games;
using PetroGlyph.Games.EawFoc.Mods;
using Validation;

namespace RepublicAtWar.DevLauncher;

internal class GameLauncher
{
    private readonly IMod _republicAtWar;
    private readonly IServiceProvider _serviceProvider;
    private readonly IGameClientFactory _clientFactory;
    private readonly ILogger? _logger;

    public GameLauncher(IMod rawDevMod, IServiceProvider serviceProvider)
    {
        Requires.NotNull(rawDevMod, nameof(rawDevMod));
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _republicAtWar = rawDevMod;
        _serviceProvider = serviceProvider;
        _clientFactory = serviceProvider.GetRequiredService<IGameClientFactory>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public void Launch(IArgumentCollection gameArguments)
    {
        var game = _republicAtWar.Game;
        if (game.Platform == GamePlatform.SteamGold)
            StartSteam();

        var client = _clientFactory.CreateClient(_republicAtWar.Game.Platform, _serviceProvider);
        _logger?.LogInformation("Starting Game...");
#if DEBUG
        _logger?.LogInformation("Game will not start in DEBUG mode");
#else 
        client.Play(_republicAtWar, gameArguments);
#endif
    }

    private void StartSteam()
    {
        var steam = _serviceProvider.GetRequiredService<ISteamWrapper>();
        if (steam.IsRunning)
            return;
        _logger?.LogInformation("Waiting for Steam...");
        steam.WaitSteamRunningAndLoggedInAsync(true).Wait();
        _logger?.LogInformation("Steam started.");
    }
}