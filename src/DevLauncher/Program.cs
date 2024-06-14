using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AET.ModVerify;
using AET.SteamAbstraction;
using AnakinRaW.ApplicationBase;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.CommonUtilities.Registry.Windows;
using AnakinRaW.CommonUtilities.SimplePipeline;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.Commons.Extensibility;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Files.ALO;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using PG.StarWarsGame.Files.MEG.Data.Archives;
using PG.StarWarsGame.Infrastructure;
using PG.StarWarsGame.Infrastructure.Clients;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Localization;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines;
using RepublicAtWar.DevLauncher.Services;
using RepublicAtWar.DevLauncher.Utilities;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace RepublicAtWar.DevLauncher;

internal class Program : CliBootstrapper
{
    protected override bool AutomaticUpdate => true;

    protected override IEnumerable<string>? AdditionalNamespacesToLogToConsole
    {
        get
        {
            yield return "AET.ModVerify";
        }
    }

    private bool HasErrors { get; set; }

    private bool HasWarning { get; set; }

    private static readonly CancellationTokenSource ApplicationCancellationTokenSource = new();

    public static int Main(string[] args)
    {
        Console.CancelKeyPress += (_, _) => ApplicationCancellationTokenSource.Cancel();
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
        Type[] optionTypes =
        [
            typeof(BuildAndRunOption), 
            typeof(InitializeLocalizationOption),
            typeof(PrepareLocalizationsOption),
            typeof(MergeLocalizationOption),
            typeof(ReleaseRepublicAtWarOption),
            typeof(VerifyOption)
        ];

        var toolResult = 0;
        var parseResult = new Parser(with =>
        {
            with.IgnoreUnknownArguments = true;
        }).ParseArguments(args, optionTypes);

        parseResult.WithParsed(o =>
        {
            Task.Run(async () =>
            {
                toolResult = await Run((DevToolsOptionBase)o, serviceCollection);
            }).Wait();


        });
        parseResult.WithNotParsed(e =>
        {
            Console.WriteLine(HelpText.AutoBuild(parseResult).ToString());
            toolResult = 0xA0;
        });
        return toolResult;
    }

    protected override void ConfigureLogging(ILoggingBuilder loggingBuilder, IFileSystem fileSystem, 
        IApplicationEnvironment applicationEnvironment)
    {
        base.ConfigureLogging(loggingBuilder, fileSystem, applicationEnvironment);

        loggingBuilder.AddFilter(level =>
        {
            if (level is LogLevel.Warning) 
                HasWarning = true;
            if (level is LogLevel.Error or LogLevel.Critical)
                HasErrors = true;
            return true;
        });
    }

    private async Task<int> Run(DevToolsOptionBase options, IServiceCollection serviceCollection)
    {
        var services = CreateAppServices(options, serviceCollection);
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());

        try
        {
            var gameFinderResult = new ModFinderService(services).FindAndAddModInCurrentDirectory();

            if (gameFinderResult.Mod is not IPhysicalMod raw)
                throw new InvalidOperationException("Unable to find physical mod Republic at War");

            IPipeline? launcherPipeline = null;

            switch (options)
            {
                case BuildAndRunOption runOptions:
                    launcherPipeline = new BuildAndRunPipeline(runOptions, raw, services);
                    break;
                case InitializeLocalizationOption:
                    new LocalizationFileService(options, services).InitializeFromDatFiles();
                    break;
                case PrepareLocalizationsOption:
                    new LocalizationFileService(options, services).CreateForeignDiffFiles();
                    break;
                case ReleaseRepublicAtWarOption releaseOptions:
                    launcherPipeline = new ReleaseRawPipeline(releaseOptions, raw, gameFinderResult.FallbackGame, services);
                    break;
                case MergeLocalizationOption:
                    new LocalizationFileService(options, services).MergeDiffsInfoFiles();
                    break;
                case VerifyOption verifyOption:
                    launcherPipeline = new BuildAndVerifyPipeline(verifyOption, raw, gameFinderResult.FallbackGame, services);
                    break;
                default:
                    throw new ArgumentException($"The option '{options.GetType().FullName}' is not implemented", nameof(options));
            }
            
            if (launcherPipeline is not null) 
                await launcherPipeline.RunAsync(ApplicationCancellationTokenSource.Token);

            if (!HasErrors && !HasWarning)
                logger?.LogInformation("DONE");
            if (HasErrors && !HasWarning)
                logger?.LogInformation("DONE with errors");
            else if (HasWarning && !HasErrors)
                logger?.LogInformation("DONE with warnings");

            return 0;
        }
        catch (Exception e)
        {
            logger?.LogError(e.Message, e);
            return e.HResult;
        }
        finally
        {
            if (HasErrors || HasWarning)
            {
                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();
            }
        }
    }

    private static IServiceProvider CreateAppServices(DevToolsOptionBase options, IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
        serviceCollection.AddSingleton<IRegistry>(_ => new WindowsRegistry());

        SteamAbstractionLayer.InitializeServices(serviceCollection);
        PetroglyphGameClients.InitializeServices(serviceCollection);
        PetroglyphGameInfrastructure.InitializeServices(serviceCollection);

        RuntimeHelpers.RunClassConstructor(typeof(IDatBuilder).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(IMegArchive).TypeHandle);
        AloServiceContribution.ContributeServices(serviceCollection);
        serviceCollection.CollectPgServiceContributions();

        PetroglyphEngineServiceContribution.ContributeServices(serviceCollection);
        ModVerifyServiceContribution.ContributeServices(serviceCollection);

        serviceCollection.AddSingleton(sp => new GitService(".", options.WarnAsError, sp));

        var forceRebuild = options is RaWBuildOption { CleanBuild: true };
        serviceCollection.AddTransient<IBinaryRequiresUpdateChecker>(sp => new TimeStampBasesUpdateChecker(forceRebuild, sp));

        serviceCollection.AddTransient(sp => new LocalizationFileWriter(options.WarnAsError, sp));
        serviceCollection.AddTransient(sp => new LocalizationFileReader(options.WarnAsError, sp));

        return serviceCollection.BuildServiceProvider();
    }
}