using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using EawModinfo.Model;
using EawModinfo.Spec;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Clients.Steam;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using PG.StarWarsGame.Infrastructure.Services;
using PG.StarWarsGame.Infrastructure.Services.Detection;

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
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _fileSystem = _serviceProvider.GetRequiredService<IFileSystem>();
        _gameFactory = _serviceProvider.GetRequiredService<IGameFactory>();
        _modFactory = _serviceProvider.GetRequiredService<IModFactory>();
        _logger = _serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public GameFinderResult FindAndAddModInCurrentDirectory()
    {
        var currentDirectory = _fileSystem.DirectoryInfo.New(Environment.CurrentDirectory);

        // Assuming the currentDir is inside a Mod's directory, we need to go up two level (Game/Mods/ModDir)
        var potentialGameDirectory = currentDirectory.Parent?.Parent;
        if (potentialGameDirectory is null)
            throw new GameException("Unable to find game installation: Wrong install path?");

        var gd = new CompositeGameDetector(new List<IGameDetector>
        {
            new DirectoryGameDetector(potentialGameDirectory, _serviceProvider),
            new SteamPetroglyphStarWarsGameDetector(_serviceProvider)
        }, _serviceProvider, true);

        var focDetectionResult = gd.Detect(new GameDetectorOptions(GameType.Foc));

        if (focDetectionResult.GameLocation is null)
            throw new GameException("Unable to find game installation: Wrong install path?");

        if (focDetectionResult.Error is not null)
            throw new GameException($"Unable to find game installation: {focDetectionResult.Error.Message}", focDetectionResult.Error);

        _logger?.LogInformation($"Found game {focDetectionResult.GameIdentity} at '{focDetectionResult.GameLocation.FullName}'");

        var foc = _gameFactory.CreateGame(focDetectionResult);

        var rawId = new ModReference(currentDirectory.FullName, ModType.Default);
        var raw = _modFactory.FromReference(foc, rawId, false);
        foc.AddMod(raw);

        var eawDetectionResult = gd.Detect(new GameDetectorOptions(GameType.EaW));
        if (eawDetectionResult.GameLocation is null)
            throw new GameException("Unable to find Empire at War installation.");
        if (eawDetectionResult.Error is not null)
            throw new GameException($"Unable to find game installation: {eawDetectionResult.Error.Message}", eawDetectionResult.Error);
        _logger?.LogInformation($"Found game {eawDetectionResult.GameIdentity} at '{eawDetectionResult.GameLocation.FullName}'");

        var eaw = _gameFactory.CreateGame(eawDetectionResult);

        return new GameFinderResult(raw, eaw);
    }
}

public readonly struct GameFinderResult(IMod mod, IGame fallbackGame)
{
    public IMod Mod { get; } = mod;

    public IGame FallbackGame { get; } = fallbackGame;
}