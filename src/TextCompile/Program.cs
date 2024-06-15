using System;
using AET.SteamAbstraction;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Registry.Windows;
using AnakinRaW.CommonUtilities.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using PG.StarWarsGame.Files.MEG.Data.Archives;
using PG.StarWarsGame.Infrastructure.Clients;
using PG.StarWarsGame.Infrastructure;
using RepublicAtWar.DevTools.Services;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PG.Commons.Extensibility;
using Microsoft.Extensions.Logging.Console;
using RepublicAtWar.DevTools.PipelineSteps.Settings;

namespace RepublicAtWar.TextCompile;

internal class TextCompile(IServiceProvider serviceProvider)
{
    public static bool HasErrors { get; set; }

    static async Task Main()
    {
        try
        {
            var services = CreateAppServices();
            await new TextCompile(services).Run();
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

        var settings = new BuildSettings
        {
            CleanBuild = true
        };

        await new CompileTextDiffsPipeline(settings, serviceProvider).RunAsync();
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