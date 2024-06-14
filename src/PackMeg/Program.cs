using System.IO.Abstractions;
using AET.SteamAbstraction;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using PG.StarWarsGame.Infrastructure.Clients;
using PG.StarWarsGame.Infrastructure;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Services;
using System.Runtime.CompilerServices;
using PG.Commons.Extensibility;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using PG.StarWarsGame.Files.MEG.Data.Archives;
using AnakinRaW.CommonUtilities.Registry.Windows;
using AnakinRaW.CommonUtilities.Registry;
using PG.StarWarsGame.Engine;
using Microsoft.Extensions.Logging;
using RepublicAtWar.DevTools.PipelineSteps.Settings;
using RepublicAtWar.DevTools.Services;

namespace PackMeg;

internal class MegPacker(IServiceProvider serviceProvider)
{
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(MegPacker));

    static async Task Main()
    {
        try
        {
            var services = CreateAppServices();
            await new MegPacker(services).Run();
        }
        finally
        {
            if (HasErrors)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Application failed with errors.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }
    }

    private async Task Run()
    {
        var gameFinderResult = new ModFinderService(serviceProvider).FindAndAddModInCurrentDirectory();

        if (gameFinderResult.Mod is not IPhysicalMod raw)
            throw new InvalidOperationException("Unable to find physical mod Republic at War");

        var fs = raw.Directory.FileSystem;
        if (!fs.Directory.Exists(fs.Path.Combine(raw.Directory.FullName, "Data")))
        {
            _logger?.LogError("Unable to find Republic at War!");
            return;
        }

        var pipeline = new PackSfxMegPipeline(raw, new BuildSettings { CleanBuild = true }, serviceProvider);
        await pipeline.RunAsync();
    }

    private static IServiceProvider CreateAppServices()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging(ConfigureLogging);

        serviceCollection.AddSingleton<IFileSystem>(new FileSystem());
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
        serviceCollection.AddSingleton<IRegistry>(new WindowsRegistry());
        
        serviceCollection.AddSingleton<IBinaryRequiresUpdateChecker>(sp => new TimeStampBasesUpdateChecker(true, sp));

        SteamAbstractionLayer.InitializeServices(serviceCollection);
        PetroglyphGameClients.InitializeServices(serviceCollection);
        PetroglyphGameInfrastructure.InitializeServices(serviceCollection);
        PetroglyphEngineServiceContribution.ContributeServices(serviceCollection);

        RuntimeHelpers.RunClassConstructor(typeof(IDatBuilder).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(IMegArchive).TypeHandle);
        serviceCollection.CollectPgServiceContributions();

        return serviceCollection.BuildServiceProvider();
    }

    private static void ConfigureLogging(ILoggingBuilder loggingBuilder)
    {
        loggingBuilder.ClearProviders();

        // ReSharper disable once RedundantAssignment
        var logLevel = LogLevel.Information;
#if DEBUG
        logLevel = LogLevel.Debug;
        loggingBuilder.AddDebug();
#endif
        loggingBuilder.AddConsole();

        loggingBuilder.AddFilter(level =>
        {
            if (level is LogLevel.Error or LogLevel.Critical)
                HasErrors = true;
            return true;
        });
    }

    public static bool HasErrors { get; set; }
}