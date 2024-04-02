using System;
using System.IO;
using System.IO.Abstractions;
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
using PG.StarWarsGame.Files.DAT.Files;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using PG.StarWarsGame.Files.MEG.Data.Archives;
using PG.StarWarsGame.Infrastructure;
using PG.StarWarsGame.Infrastructure.Clients;
using PG.StarWarsGame.Infrastructure.Clients.Steam;
using PG.StarWarsGame.Infrastructure.Mods;
using PG.StarWarsGame.Infrastructure.Services.Detection;
using RepublicAtWar.DevLauncher.Localization;
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

        var raw = new ModFinderService(services).FindAndAddModInCurrentDirectory() as IPhysicalMod;
        if (raw is null)
            throw new InvalidOperationException("Unable to find physical mod Republic at War");

        try
        {

            Parser.Default.ParseArguments<BuildAndRunOption, DatToLocalizationOption>(args)
                .WithParsed<DatToLocalizationOption>(o =>
                {
                    CreateLocalizationFiles(raw, services);
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

    private void CreateLocalizationFiles(IPhysicalMod raw, IServiceProvider services)
    {
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger<Program>();

        var fs = services.GetRequiredService<IFileSystem>();

        var writer = new LocalizationFileWriter(raw, services);
        writer.DatToLocalizationFile("Data\\Text\\MasterTextFile_ENGLISH.DAT");

        var locFileFs = fs.FileStream.New(fs.Path.Combine(raw.Directory.FullName, "Data\\Text\\MasterTextFile_ENGLISH.txt"), FileMode.Open);
        var reader = new LocalizationFileReaderReader(services);

        var locFile = reader.FromStream(locFileFs);

        var builder = new EmpireAtWarMasterTextFileBuilder(false, services);

        foreach (var entry in locFile.Entries)
        {
            var result = builder.AddEntry(entry.Key, entry.Value);
            if (!result.Added)
            {
                // TODO: Currently 'TEXT_ORDER66_UTAPAU_OBJECTIVE_02 ' and 'TEXT_TOOLTIP_CLONETROOPER_MEDIC_P1_SQUAD '
                // are not valid because of trailing space

                logger?.LogWarning($"Unable to add KEY '{entry.Key}' to the DAT file.");
            }
        }

        builder.Build(new DatFileInformation { FilePath = fs.Path.Combine(raw.Directory.FullName, "Data\\Text\\Other.dat") }, true);
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

[Verb("dat2loc")]
public class DatToLocalizationOption : UpdaterCommandLineOptions;