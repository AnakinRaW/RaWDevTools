﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Clients.Steam;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using PG.StarWarsGame.Infrastructure.Services;
using PG.StarWarsGame.Infrastructure.Services.Detection;

namespace RepublicAtWar.DevTools.Services;

public class ModFinderService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger? _logger;
    private readonly IGameFactory _gameFactory;
    private readonly IModFactory _modFactory;
    private readonly IModFinder _modFinder;

    public ModFinderService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _fileSystem = _serviceProvider.GetRequiredService<IFileSystem>();
        _gameFactory = _serviceProvider.GetRequiredService<IGameFactory>();
        _modFactory = _serviceProvider.GetRequiredService<IModFactory>();
        _modFinder = _serviceProvider.GetRequiredService<IModFinder>();
        _logger = _serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public GameFinderResult FindAndAddModInCurrentDirectory()
    {
        var currentDirectory = _fileSystem.DirectoryInfo.New(Environment.CurrentDirectory);

        // Assuming the currentDir is inside a RepublicAtWar's directory, we need to go up two level (Game/Mods/ModDir)
        var potentialGameDirectory = currentDirectory.Parent?.Parent;
        if (potentialGameDirectory is null)
            throw new GameException("Unable to find game installation: Wrong install path?");

        var gd = new CompositeGameDetector(new List<IGameDetector>
        {
            new DirectoryGameDetector(potentialGameDirectory, _serviceProvider),
            new SteamPetroglyphStarWarsGameDetector(_serviceProvider)
        }, _serviceProvider, true);

        var focDetectionResult = gd.Detect(GameType.Foc);
        
        if (focDetectionResult.GameLocation is null)
            throw new GameException("Unable to find game installation: Wrong install path?");

        _logger?.LogInformation($"Found game {focDetectionResult.GameIdentity} at '{focDetectionResult.GameLocation.FullName}'");

        var foc = _gameFactory.CreateGame(focDetectionResult, CultureInfo.InvariantCulture);

        if (!_fileSystem.Directory.Exists(_fileSystem.Path.Combine(currentDirectory.FullName, "Data")))
            throw new InvalidOperationException("Unable to find physical mod Republic at War");

        var rawId = _modFinder.FindMods(foc, currentDirectory).FirstOrDefault();
        if (rawId is null)
            throw new InvalidOperationException("Unable to find physical mod Republic at War");

        var raw = _modFactory.CreatePhysicalMod(foc, rawId, CultureInfo.InvariantCulture);
        foc.AddMod(raw);

        var eawDetectionResult = gd.Detect(GameType.Eaw);
        if (eawDetectionResult.GameLocation is null)
            throw new GameException("Unable to find Empire at War installation.");
        _logger?.LogInformation($"Found game {eawDetectionResult.GameIdentity} at '{eawDetectionResult.GameLocation.FullName}'");

        var eaw = _gameFactory.CreateGame(eawDetectionResult, CultureInfo.InvariantCulture);

        return new GameFinderResult(raw, eaw);
    }
}

public readonly struct GameFinderResult(IPhysicalMod republicAtWar, IGame fallbackGame)
{
    public IPhysicalMod RepublicAtWar { get; } = republicAtWar;

    public IGame FallbackGame { get; } = fallbackGame;
}