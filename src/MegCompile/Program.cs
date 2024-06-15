using System;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AET.SteamAbstraction;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.CommonUtilities.Registry.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using PG.Commons.Extensibility;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using PG.StarWarsGame.Files.MEG.Data.Archives;
using PG.StarWarsGame.Infrastructure;
using PG.StarWarsGame.Infrastructure.Clients;
using RepublicAtWar.DevTools.PipelineSteps.Settings;
using RepublicAtWar.DevTools.Services;

namespace RepublicAtWar.MegCompile;

internal class MegCompile(IServiceProvider serviceProvider)
{
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(MegCompile));

    public static bool HasErrors { get; set; }

    static async Task Main()
    {
        try
        {
            var services = CreateAppServices();
            await new MegCompile(services).Run();
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
        var pipeline = new PackSfxMegPipeline(gameFinderResult.RepublicAtWar, new BuildSettings { CleanBuild = true }, serviceProvider);
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

        loggingBuilder.AddFilter<ConsoleLoggerProvider>((category, level) =>
        {
            if (level < logLevel)
                return false;
            if (string.IsNullOrEmpty(category))
                return false;

            if (category!.StartsWith("RepublicAtWar."))
                return true;

            return false;
        });
    }
}