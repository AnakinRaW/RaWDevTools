using System;
using System.Reflection;
using AET.SteamAbstraction;
using AnakinRaW.ApplicationBase;
using AnakinRaW.ApplicationBase.Options;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.CommonUtilities.Registry.Windows;
using AnakinRaW.CommonUtilities.SimplePipeline;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.Commons.Extensibility;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using PG.StarWarsGame.Files.MEG.Data.Archives;
using PG.StarWarsGame.Infrastructure;
using PG.StarWarsGame.Infrastructure.Clients;
using PG.StarWarsGame.Infrastructure.Clients.Steam;
using PG.StarWarsGame.Infrastructure.Mods;
using PG.StarWarsGame.Infrastructure.Services.Detection;
using RepublicAtWar.DevLauncher.Pipelines;
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
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger<Program>();

        if (new ModFinderService(services).FindAndAddModInCurrentDirectory() is not IPhysicalMod raw)
            throw new InvalidOperationException("Unable to find physical mod Republic at War");

        try
        {

            Parser.Default.ParseArguments<BuildAndRunOption, InitializeLocalizationOption>(args)
                .WithParsed<InitializeLocalizationOption>(o =>
                {
                    new LocalizationInitializer(services).Run();
                })
                .WithParsed<BuildAndRunOption>(_ =>
                {
                    BuildAndRun(raw, services);
                });

            return 0;

        }
        catch (StepFailureException e)
        {
            logger?.LogError(e, $"Building Mod Failed: {e.Message}");
            return e.HResult;
        }
    }

    private void BuildAndRun(IPhysicalMod raw, IServiceProvider services)
    {
        var pipeline = new RawDevLauncherPipeline(raw, services);
        pipeline.Run();
    }

    private static IServiceProvider CreateServices(IServiceCollection serviceCollection)
    {
        SteamAbstractionLayer.InitializeServices(serviceCollection);
        PetroglyphGameClients.InitializeServices(serviceCollection);
        PetroglyphGameInfrastructure.InitializeServices(serviceCollection);

        Console.WriteLine(typeof(IDatBuilder));
        Console.WriteLine(typeof(IMegArchive));

        serviceCollection.CollectPgServiceContributions();

        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));

        serviceCollection.AddTransient<IGameDetector>(sp => new SteamPetroglyphStarWarsGameDetector(sp));

        serviceCollection.AddTransient<IBinaryRequiresUpdateChecker>(sp => new TimeStampBasesUpdateChecker(sp));
        
        serviceCollection.AddTransient<IMegPackerService>(sp => new MegPackerService(sp));

        return serviceCollection.BuildServiceProvider();
    }
}

[Verb("buildRun", true)]
public class BuildAndRunOption : UpdaterCommandLineOptions;

[Verb("initLoc")]
public class InitializeLocalizationOption : UpdaterCommandLineOptions;