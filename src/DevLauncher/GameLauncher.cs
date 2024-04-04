using System;
using AET.SteamAbstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Clients;
using PG.StarWarsGame.Infrastructure.Clients.Arguments;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;

namespace RepublicAtWar.DevLauncher;

internal class GameLauncher
{
    private readonly BuildAndRunOption _options;
    private readonly IMod _republicAtWar;
    private readonly IServiceProvider _serviceProvider;
    private readonly IGameClientFactory _clientFactory;
    private readonly ILogger? _logger;

    public GameLauncher(BuildAndRunOption options, IMod rawDevMod, IServiceProvider serviceProvider)
    {
        _options = options;
        _republicAtWar = rawDevMod ?? throw new ArgumentNullException(nameof(rawDevMod));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
        _logger?.LogWarning("Game will not start in DEBUG mode");
#else 
        if (!_options.SkipRun)
            client.Play(_republicAtWar, gameArguments);
#endif
    }

    private void StartSteam()
    {
        var steam = _serviceProvider.GetRequiredService<ISteamWrapperFactory>().CreateWrapper();
        if (steam.IsRunning)
            return;
        _logger?.LogInformation("Waiting for Steam...");
        steam.WaitSteamRunningAndLoggedInAsync(true).Wait();
        _logger?.LogInformation("Steam started.");
    }
}