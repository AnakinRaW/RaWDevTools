using System;
using System.IO.Abstractions;
using System.Reflection;
using AnakinRaW.ApplicationBase;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.CommonUtilities.Registry.Windows;
using AnakinRaW.CommonUtilities.SimplePipeline;
using EawModinfo.Model;
using EawModinfo.Spec;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PetroGlyph.Games.EawFoc.Clients.Steam;
using PetroGlyph.Games.EawFoc.Clients;
using PetroGlyph.Games.EawFoc;
using PetroGlyph.Games.EawFoc.Clients.Arguments;
using PetroGlyph.Games.EawFoc.Games;
using PetroGlyph.Games.EawFoc.Services;
using PetroGlyph.Games.EawFoc.Services.Dependencies;
using PetroGlyph.Games.EawFoc.Services.Detection;
using PetroGlyph.Games.EawFoc.Services.Name;

namespace RepublicAtWar.DevLauncher;

internal class Program : CliBootstrapper
{
    protected override bool AutomaticUpdate => true;

    public static int Main(string[] args)
    {
        return new Program().Run(args);
    }

    protected override IApplicationEnvironment CreateEnvironment(IServiceProvider serviceProvider)
    {
        return new DevLauncherEnvironment(Assembly.GetExecutingAssembly(), serviceProvider);
    }

    protected override IRegistry CreateRegistry()
    {
        return new WindowsRegistry();
    }

    protected override int ExecuteAfterUpdate(string[] args, IServiceCollection serviceCollection)
    {
        var services = CreateServices(serviceCollection);

        var fileSystem = services.GetRequiredService<IFileSystem>();
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());

        var currentDirectory = fileSystem.DirectoryInfo.New(Environment.CurrentDirectory);
       
        // Assuming the currentDir is inside a Mod's directory, we need to go up two level (Game/Mods/ModDir)
        var potentialGameDirectory = currentDirectory.Parent?.Parent;
        if (potentialGameDirectory is null)
            throw new GameException("Unable to find game installation: Wrong install path?");

        var gd = new DirectoryGameDetector(potentialGameDirectory, services);
        var detectedGame = gd.Detect(new GameDetectorOptions(GameType.Foc));

        if (detectedGame.GameLocation is null)
            throw new GameException("Unable to find game installation: Wrong install path?");

        if (detectedGame.Error is not null)
            throw new GameException($"Unable to find game installation: {detectedGame.Error.Message}", detectedGame.Error);

        logger?.LogInformation($"Found game {detectedGame.GameIdentity} at '{detectedGame.GameLocation.FullName}'");

        var gameFactory = new GameFactory(services);
        var game = gameFactory.CreateGame(detectedGame);

        var rawId = new ModReference(currentDirectory.FullName, ModType.Default);

        var raw = new ModFactory(services).FromReference(game, rawId, false);
        game.AddMod(raw);

        try
        {
            var pipeline = new RawDevLauncherPipeline(raw, services);
            pipeline.Run();
            return 0;
        }
        catch (StepFailureException e)
        {
            logger?.LogError(e, $"Building Mod Failed: {e.Message}");
            return e.HResult;
        }
    }

    private static IServiceProvider CreateServices(IServiceCollection serviceCollection)
    {
        PetroglyphGameInfrastructureLibrary.InitializeLibraryWithDefaultServices(serviceCollection);
        PetroglyphClientsLibrary.InitializeLibraryWithDefaultServices(serviceCollection);
        PetroglyphWindowsSteamClientsLibrary.InitializeLibraryWithDefaultServices(serviceCollection);

        serviceCollection.AddTransient<IGameDetector>(sp => new SteamPetroglyphStarWarsGameDetector(sp));
        serviceCollection.AddTransient<IGameFactory>(sp => new GameFactory(sp));
        
        serviceCollection.AddTransient<IModReferenceFinder>(sp => new FileSystemModFinder(sp));
        serviceCollection.AddTransient<IModFactory>(sp => new ModFactory(sp));
        serviceCollection.AddTransient<IModReferenceLocationResolver>(sp => new ModReferenceLocationResolver(sp));
        serviceCollection.AddTransient<IModNameResolver>(sp => new DirectoryModNameResolver(sp));
        serviceCollection.AddTransient<IDependencyResolver>(sp => new ModDependencyResolver(sp));
        serviceCollection.AddTransient<IGameClientFactory>(sp => new DefaultGameClientFactory(sp));
        serviceCollection.AddTransient<IModArgumentListFactory>(sp => new ModArgumentListFactory(sp));
        serviceCollection.AddTransient<IArgumentCollectionBuilder>(_ => new KeyBasedArgumentCollectionBuilder());

        return serviceCollection.BuildServiceProvider();
    }
}