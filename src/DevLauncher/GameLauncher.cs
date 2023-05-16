using System;
using EawModinfo.Model;
using Microsoft.Extensions.DependencyInjection;
using PetroGlyph.Games.EawFoc.Clients;
using PetroGlyph.Games.EawFoc.Clients.Arguments;
using PetroGlyph.Games.EawFoc.Clients.Arguments.GameArguments;
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

    public GameLauncher(IMod rawDevMod, IServiceProvider serviceProvider)
    {
        Requires.NotNull(rawDevMod, nameof(rawDevMod));
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _republicAtWar = rawDevMod;
        _serviceProvider = serviceProvider;
        _clientFactory = serviceProvider.GetRequiredService<IGameClientFactory>();
    }

    public void Launch()
    {
        var game = _republicAtWar.Game;
        if (game.Platform == GamePlatform.SteamGold)
            StartSteam();

        var gameArgs = CreateGameArgs();

        var client = _clientFactory.CreateClient(_republicAtWar.Game.Platform, _serviceProvider);
        client.Play(_republicAtWar, gameArgs);
    }

    private IArgumentCollection CreateGameArgs()
    {
        var modArgFactory = _serviceProvider.GetRequiredService<IModArgumentListFactory>();
        var modArgs = modArgFactory.BuildArgumentList(_republicAtWar, false);
        var gameArgsBuilder = _serviceProvider.GetRequiredService<IArgumentCollectionBuilder>();
        gameArgsBuilder
            .Add(new WindowedArgument())
            .Add(new LanguageArgument(LanguageInfo.Default))
            .Add(modArgs);
        
        return gameArgsBuilder.Build();
    }

    private void StartSteam()
    {
        var steam = _serviceProvider.GetRequiredService<ISteamWrapper>();
        steam.WaitSteamRunningAndLoggedInAsync(true).Wait();
    }
}