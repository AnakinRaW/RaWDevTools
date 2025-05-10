using AET.ModVerify.Reporting.Reporters;
using AET.SteamAbstraction;
using AnakinRaW.ApplicationBase;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.CommonUtilities.Registry.Windows;
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
using RepublicAtWar.DevLauncher.Services;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Utilities;
using PG.StarWarsGame.Engine.Xml.Parsers;
using PG.StarWarsGame.Files.XML.Parsers;
using RepublicAtWar.DevLauncher.Update;
using Serilog.Sinks.SystemConsole.Themes;
using Testably.Abstractions;
using ILogger = Serilog.ILogger;

namespace RepublicAtWar.DevLauncher;

public static class MainClass
{
    // In some build scenarios we cannot have the Main method in a class that inherits a type form an embedded assembly.
    // This might result in FileNotFoundExceptions when the CLR is trying to load the type that contains the Main method.
    public static Task<int> Main(string[] args)
    {
        return new Program().StartAsync(args);
    }
}

internal class Program : SelfUpdateableAppLifecycle
{
    private static readonly string EngineParserNamespace = typeof(XmlObjectParser<>).Namespace!;
    private static readonly string ParserNamespace = typeof(PetroglyphXmlFileParser<>).Namespace!;
    private static readonly string DevLauncherRootNamespace = typeof(Program).Namespace!;
    private static readonly string DevLauncherUpdateNamespace = typeof(RawDevLauncherUpdater).Namespace!;
    
    protected override ApplicationEnvironment CreateAppEnvironment()
    {
        return new DevLauncherEnvironment(Assembly.GetExecutingAssembly(), FileSystem);
    }

    protected override IFileSystem CreateFileSystem()
    {
        return new RealFileSystem();
    }

    protected override IRegistry CreateRegistry()
    {
        return new WindowsRegistry();
    }

    protected override async Task<int> RunAppAsync(string[] args, IServiceProvider appServiceProvider)
    {
        using (new UnhandledExceptionHandler(appServiceProvider))
        using (new UnobservedTaskExceptionHandler(appServiceProvider))
            return await new RawDevLauncher(UpdatableApplicationEnvironment!, appServiceProvider).RunAsync(args);
    }

    protected override void ResetApp(Microsoft.Extensions.Logging.ILogger? logger)
    {
        logger?.LogDebug("Resetting Application");
        var deleteResult = ApplicationEnvironment.ApplicationLocalDirectory.TryDeleteWithRetry();
        if (!deleteResult)
            logger?.LogWarning("Failed to delete application local directory.");
        ApplicationEnvironment.ApplicationLocalDirectory.Create();
    }

    protected override void CreateAppServices(IServiceCollection services, IReadOnlyCollection<string> args)
    {
        services.AddLogging(ConfigureLogging);

        services.AddSingleton<IHashingService>(sp => new HashingService(sp));

        SteamAbstractionLayer.InitializeServices(services);
        SteamPetroglyphStarWarsGameClients.InitializeServices(services);
        PetroglyphGameInfrastructure.InitializeServices(services);

        services.SupportDAT();
        services.SupportMTD();
        services.SupportMEG();
        services.SupportXML();
        services.SupportALO();
        PetroglyphCommons.ContributeServices(services);

        PetroglyphEngineServiceContribution.ContributeServices(services);
        services.RegisterJsonReporter();
        services.RegisterTextFileReporter();

        services.AddSingleton(sp => new GitService(".", sp));

        services.MakeAppUpdateable(
            UpdatableApplicationEnvironment!,
            sp => new CosturaApplicationProductService(ApplicationEnvironment, sp),
            sp => new JsonManifestLoader(sp),
            sc => { sc.AddSingleton<ILockedFileInteractionHandler>(new CosturaLockedFileHandler()); });
    }

    private void ConfigureLogging(ILoggingBuilder loggingBuilder)
    {
        loggingBuilder.ClearProviders();

        // ReSharper disable once RedundantAssignment
        var logLevel = LogEventLevel.Information;
#if DEBUG
        logLevel = LogEventLevel.Debug;
        loggingBuilder.AddDebug();
#endif

        var fileLogger = SetupFileLogging(ApplicationEnvironment.ApplicationLocalPath, logLevel);
        loggingBuilder.AddSerilog(fileLogger);

        var cLogger = new LoggerConfiguration()
            .WriteTo.Console(
                logLevel,
                theme: AnsiConsoleTheme.Code,
                outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .Filter.ByIncludingOnly(x =>
            {
                if (!x.Properties.TryGetValue("SourceContext", out var value))
                    return true;

                var source = value.ToString().AsSpan().Trim('\"');

                if (!source.StartsWith(DevLauncherRootNamespace.AsSpan()) && !source.StartsWith("RepublicAtWar.".AsSpan())) 
                    return false;

                if (source.StartsWith(DevLauncherUpdateNamespace.AsSpan()))
                    return false;
                
                return true;

            })
            .MinimumLevel.Is(logLevel)
            .CreateLogger();
        
        loggingBuilder.AddSerilog(cLogger);
    }

    private ILogger SetupFileLogging(string path, LogEventLevel minLevel)
    {
        var logPath = FileSystem.Path.Combine(path, "ModVerify_log.txt");

        return new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Is(minLevel)
            .Filter.ByExcluding(IsXmlParserLogging)
            .WriteTo.Async(c =>
            {
                c.RollingFile(
                    logPath,
                    outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}");
            })
            .CreateLogger();
    }

    private static bool IsXmlParserLogging(LogEvent logEvent)
    {
        return Matching.FromSource(ParserNamespace)(logEvent) || Matching.FromSource(EngineParserNamespace)(logEvent);
    }
}