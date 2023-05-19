using System;
using System.Reflection;
using AnakinRaW.ApplicationBase;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.CommonUtilities.Registry.Windows;
using AnakinRaW.CommonUtilities.SimplePipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PetroGlyph.Games.EawFoc.Clients.Steam;
using PetroGlyph.Games.EawFoc.Clients;
using PetroGlyph.Games.EawFoc;
using PetroGlyph.Games.EawFoc.Clients.Arguments;
using PetroGlyph.Games.EawFoc.Services;
using PetroGlyph.Games.EawFoc.Services.Dependencies;
using PetroGlyph.Games.EawFoc.Services.Detection;
using PetroGlyph.Games.EawFoc.Services.Name;
using RepublicAtWar.DevLauncher.Services;

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
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        var raw = new ModFinderService(services).FindAndAddModInCurrentDirectory();

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