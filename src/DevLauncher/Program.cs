using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AET.ModVerify.Reporting.Reporters;
using AET.SteamAbstraction;
using AnakinRaW.ApplicationBase;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.CommonUtilities.Registry.Windows;
using AnakinRaW.CommonUtilities.SimplePipeline;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.Commons;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Files.ALO;
using PG.StarWarsGame.Files.DAT;
using PG.StarWarsGame.Files.MEG;
using PG.StarWarsGame.Files.MTD;
using PG.StarWarsGame.Files.XML;
using PG.StarWarsGame.Infrastructure;
using PG.StarWarsGame.Infrastructure.Clients.Steam;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines;
using RepublicAtWar.DevLauncher.Pipelines.Actions;
using RepublicAtWar.DevLauncher.Pipelines.Settings;
using RepublicAtWar.DevLauncher.Services;
using RepublicAtWar.DevTools.Services;
using RepublicAtWar.DevTools.Steps.Settings;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace RepublicAtWar.DevLauncher;

internal class Program : CliBootstrapper
{
    private const string EngineXmlParserNamespace = "PG.StarWarsGame.Engine.Xml.Parsers";
    private const string XmlParserNamespace = "PG.StarWarsGame.Files.XML.Parsers.Primitives";

    protected override bool AutomaticUpdate => true;

    protected override IEnumerable<string>? AdditionalNamespacesToLogToConsole
    {
        get
        {
            yield return "RepublicAtWar";
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
        
        SetupXmlParseLogging(loggingBuilder, fileSystem);

        loggingBuilder.AddFilter(level =>
        {
            if (level is LogLevel.Warning) 
                HasWarning = true;
            if (level is LogLevel.Error or LogLevel.Critical)
                HasErrors = true;
            return true;
        });
    }

    protected override bool ExcludeFromGlobalLogging(LogEvent arg)
    {
        return IsXmlParserLogging(arg);
    }

    private void SetupXmlParseLogging(ILoggingBuilder loggingBuilder, IFileSystem fileSystem)
    {
        const string xmlParseLogFileName = "XmlParseLog.txt";

        fileSystem.File.TryDeleteWithRetry(xmlParseLogFileName);

        var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Warning()
            .Filter.ByIncludingOnly(IsXmlParserLogging)
            .WriteTo.File(xmlParseLogFileName, outputTemplate: "[{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}")
            .CreateLogger();

        loggingBuilder.AddSerilog(logger);
    }


    private static bool IsXmlParserLogging(LogEvent logEvent)
    {
        return Matching.FromSource(XmlParserNamespace)(logEvent) || Matching.FromSource(EngineXmlParserNamespace)(logEvent);
    }


    private async Task<int> Run(DevToolsOptionBase options, IServiceCollection serviceCollection)
    {
        var services = CreateAppServices(options, serviceCollection);
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());

        try
        {
            var gameFinderResult = new ModFinderService(services).FindAndAddModInCurrentDirectory();

            IPipeline launcherPipeline;

            switch (options)
            {
                case BuildAndRunOption buildRun:
                    launcherPipeline = CreateBuildRunPipeline(buildRun, gameFinderResult, services);
                    break;
                case InitializeLocalizationOption:
                    launcherPipeline = new InitializeLocalizationAction(services);
                    break;
                case PrepareLocalizationsOption:
                    launcherPipeline = new CreateLocalizationDiffsAction(services);
                    break;
                case ReleaseRepublicAtWarOption rawRelease:
                    launcherPipeline = CreateReleasePipeline(rawRelease, gameFinderResult, services);
                    break;
                case MergeLocalizationOption:
                    launcherPipeline = new MergeLocalizationsAction(services);
                    break;
                case VerifyOption verifyOption:
                    launcherPipeline = CreateBuildVerifyPipeline(verifyOption, gameFinderResult, services);
                    break;
                default:
                    throw new ArgumentException($"The option '{options.GetType().FullName}' is not implemented",
                        nameof(options));
            }

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

    private static IPipeline CreateBuildVerifyPipeline(VerifyOption options, GameFinderResult gameFinderResult, IServiceProvider services)
    {
        var buildSettings = new BuildSettings
        {
            WarnAsError = options.WarnAsError,
            CleanBuild = options.CleanBuild
        };
        return new BuildAndVerifyPipeline(gameFinderResult.RepublicAtWar, gameFinderResult.FallbackGame, buildSettings, services);
    }

    private static IPipeline CreateBuildRunPipeline(BuildAndRunOption options, GameFinderResult gameFinderResult, IServiceProvider services)
    {
        var buildSettings = new BuildSettings
        {
            CleanBuild = options.CleanBuild,
            WarnAsError = options.WarnAsError
        };
        var launchSettings = new LaunchSettings
        {
            RunGame = !options.SkipRun,
            Debug = options.Debug,
            Windowed = options.Windowed
        };
        return new BuildAndRunPipeline(gameFinderResult.RepublicAtWar, buildSettings, launchSettings, services);
    }

    private static IPipeline CreateReleasePipeline(ReleaseRepublicAtWarOption options, GameFinderResult gameFinderResult, IServiceProvider services)
    {
        var buildSettings = new BuildSettings
        {
            CleanBuild = options.CleanBuild,
            WarnAsError = options.WarnAsError
        };
        var releaseSettings = new ReleaseSettings
        {
            UploaderDirectory = options.UploaderDirectory,
            WarnAsError = options.WarnAsError
        }; 
        return new ReleaseRawPipeline(gameFinderResult.RepublicAtWar, gameFinderResult.FallbackGame, buildSettings, releaseSettings, services);
    }

    private static IServiceProvider CreateAppServices(DevToolsOptionBase options, IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
        serviceCollection.AddSingleton<IRegistry>(_ => new WindowsRegistry());

        SteamAbstractionLayer.InitializeServices(serviceCollection);
        SteamPetroglyphStarWarsGameClients.InitializeServices(serviceCollection);
        PetroglyphGameInfrastructure.InitializeServices(serviceCollection);

        serviceCollection.SupportDAT();
        serviceCollection.SupportMTD();
        serviceCollection.SupportMEG();
        serviceCollection.SupportXML();
        serviceCollection.SupportALO();
        PetroglyphCommons.ContributeServices(serviceCollection);

        PetroglyphEngineServiceContribution.ContributeServices(serviceCollection);
        serviceCollection.RegisterJsonReporter();
        serviceCollection.RegisterTextFileReporter();

        serviceCollection.AddSingleton(sp => new GitService(".", options.WarnAsError, sp));

        var forceRebuild = options is RaWBuildOption { CleanBuild: true };
        serviceCollection.AddSingleton<IBinaryRequiresUpdateChecker>(sp => new TimeStampBasesUpdateChecker(forceRebuild, sp));

        return serviceCollection.BuildServiceProvider();
    }
}