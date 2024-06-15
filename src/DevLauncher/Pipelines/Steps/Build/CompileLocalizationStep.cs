using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Files.DAT.Files;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using RepublicAtWar.DevLauncher.Localization;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Utilities;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps.Build;

internal class CompileLocalizationStep(IServiceProvider serviceProvider, RaWBuildOption buildOption) : PipelineStep(serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(CompileLocalizationStep));

    protected override void RunCore(CancellationToken token)
    {
        var localizationFiles = _fileSystem.Directory.EnumerateFiles("Data\\Text", "MasterTextFile_*.txt");

        foreach (var localizationFile in localizationFiles)
            CompileDatFromLocalizationFile(localizationFile);
    }

    private void CompileDatFromLocalizationFile(string file)
    {
        var datFilePath = _fileSystem.Path.ChangeExtension(file, "dat");
        var datFileName = _fileSystem.Path.GetFileName(datFilePath);

        var updateChecker = Services.GetRequiredService<IBinaryRequiresUpdateChecker>();
        if (!buildOption.CleanBuild && !updateChecker.RequiresUpdate(datFilePath, new List<string> { file }))
        {
            _logger?.LogDebug($"DAT data '{datFileName}' is already up to date. Skipping build.");
            return;
        }

        _logger?.LogInformation($"Writing DAT data '{datFileName}'...");

        using var reader = new LocalizationFileReader(file, false, Services);
        var fileModel = reader.Read();

        using var builder = new EmpireAtWarMasterTextBuilder(false, Services);

        foreach (var entry in fileModel.Entries)
        {
            var result = builder.AddEntry(entry.Key, entry.Value);
            if (!result.Added)
                _logger?.LogWarning($"Unable to add KEY '{entry.Key}' to the DAT for language {fileModel.Language}: {result.Message}");
        }

        builder.Build(new DatFileInformation { FilePath = _fileSystem.Path.GetFullPath(datFilePath) }, true);

        _logger?.LogInformation($"Finished writing DAT data for language {fileModel.Language}");
    }
}