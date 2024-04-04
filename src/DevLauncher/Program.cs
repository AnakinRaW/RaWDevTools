using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using AET.SteamAbstraction;
using AnakinRaW.ApplicationBase;
using AnakinRaW.ApplicationBase.Options;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.CommonUtilities.Registry.Windows;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.Commons.Extensibility;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using PG.StarWarsGame.Files.MEG.Data.Archives;
using PG.StarWarsGame.Infrastructure;
using PG.StarWarsGame.Infrastructure.Clients;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Localization;
using RepublicAtWar.DevLauncher.Pipelines;
using RepublicAtWar.DevLauncher.Services;
using RepublicAtWar.DevLauncher.Utilities;

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
        Type[] optionTypes =
        [
            typeof(BuildAndRunOption), 
            typeof(InitializeLocalizationOption),
            typeof(UpdateLocalizationFilesOption),
            typeof(ReleaseRepublicAtWarOption)
        ];

        var toolResult = 0;
        Parser.Default.ParseArguments(args, optionTypes)
            .WithParsed(o => { toolResult = Run((DevToolsOptionBase)o, serviceCollection); })
            .WithNotParsed(_ => toolResult = 160);
        return toolResult;
    }

    private int Run(DevToolsOptionBase options, IServiceCollection serviceCollection)
    {
        var services = CreateAppServices(options, serviceCollection);
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());

        try
        {
            if (new ModFinderService(services).FindAndAddModInCurrentDirectory() is not IPhysicalMod raw)
                throw new InvalidOperationException("Unable to find physical mod Republic at War");

            switch (options)
            {
                case BuildAndRunOption runOptions:
                    new RawDevLauncherPipeline(runOptions, raw, services).Run();
                    break;
                case InitializeLocalizationOption:
                    new LocalizationFileService(options, services).InitializeFromDatFiles();
                    break;
                case UpdateLocalizationFilesOption:
                    new LocalizationFileService(options, services).UpdateNonEnglishFiles();
                    break;
                default:
                    throw new ArgumentException(nameof(options));
            }

            logger?.LogInformation("DONE");
            return 0;
        }
        catch (Exception e)
        {
            logger?.LogError(e.Message, e);
            return e.HResult;
        }
        finally
        {
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }
    }

    private static IServiceProvider CreateAppServices(DevToolsOptionBase options, IServiceCollection serviceCollection)
    {
        SteamAbstractionLayer.InitializeServices(serviceCollection);
        PetroglyphGameClients.InitializeServices(serviceCollection);
        PetroglyphGameInfrastructure.InitializeServices(serviceCollection);

        RuntimeHelpers.RunClassConstructor(typeof(IDatBuilder).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(IMegArchive).TypeHandle);

        serviceCollection.CollectPgServiceContributions();

        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));

        serviceCollection.AddTransient<IBinaryRequiresUpdateChecker>(sp => new TimeStampBasesUpdateChecker(sp));

        serviceCollection.AddTransient(sp => new MegPackerService(sp));

        serviceCollection.AddTransient(sp => new LocalizationFileWriter(options.WarnAsError, sp));
        serviceCollection.AddTransient(sp => new LocalizationFileReader(options.WarnAsError, sp));

        return serviceCollection.BuildServiceProvider();
    }
}

public abstract class DevToolsOptionBase : UpdaterCommandLineOptions
{
    [Option("warnAsError")]
    public bool WarnAsError { get; init; }
}

[Verb("buildRun", true)]
public sealed class BuildAndRunOption : DevToolsOptionBase
{
    [Option('w', "windowed", Default = false)]
    public bool Windowed { get; init; }

    [Option("skipRun")]
    public bool SkipRun { get; init; }
}

[Verb("initLoc")]
public sealed class InitializeLocalizationOption : DevToolsOptionBase;

[Verb("updateLoc")]
public sealed class UpdateLocalizationFilesOption : DevToolsOptionBase;

[Verb("release")]
public sealed class ReleaseRepublicAtWarOption : DevToolsOptionBase;