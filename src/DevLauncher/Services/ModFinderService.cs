using System;
using System.IO.Abstractions;
using EawModinfo.Model;
using EawModinfo.Spec;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PetroGlyph.Games.EawFoc.Games;
using PetroGlyph.Games.EawFoc.Mods;
using PetroGlyph.Games.EawFoc.Services;
using PetroGlyph.Games.EawFoc.Services.Detection;
using Validation;

namespace RepublicAtWar.DevLauncher.Services;

internal class ModFinderService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger? _logger;
    private readonly IGameFactory _gameFactory;
    private readonly IModFactory _modFactory;

    public ModFinderService(IServiceProvider serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _serviceProvider = serviceProvider;
        _fileSystem = _serviceProvider.GetRequiredService<IFileSystem>();
        _gameFactory = _serviceProvider.GetRequiredService<IGameFactory>();
        _modFactory = _serviceProvider.GetRequiredService<IModFactory>();
        _logger = _serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public IMod FindAndAddModInCurrentDirectory()
    {
        var currentDirectory = _fileSystem.DirectoryInfo.New(Environment.CurrentDirectory);

        // Assuming the currentDir is inside a Mod's directory, we need to go up two level (Game/Mods/ModDir)
        var potentialGameDirectory = currentDirectory.Parent?.Parent;
        if (potentialGameDirectory is null)
            throw new GameException("Unable to find game installation: Wrong install path?");

        var gd = new DirectoryGameDetector(potentialGameDirectory, _serviceProvider);
        var detectedGame = gd.Detect(new GameDetectorOptions(GameType.Foc));

        if (detectedGame.GameLocation is null)
            throw new GameException("Unable to find game installation: Wrong install path?");

        if (detectedGame.Error is not null)
            throw new GameException($"Unable to find game installation: {detectedGame.Error.Message}", detectedGame.Error);

        _logger?.LogInformation($"Found game {detectedGame.GameIdentity} at '{detectedGame.GameLocation.FullName}'");

        var game = _gameFactory.CreateGame(detectedGame);

        var rawId = new ModReference(currentDirectory.FullName, ModType.Default);

        var raw = _modFactory.FromReference(game, rawId, false);
        game.AddMod(raw);

        return raw;
    }
}