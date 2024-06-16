using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RepublicAtWar.DevTools.PipelineSteps.Settings;
using RepublicAtWar.DevTools.Services;

namespace RepublicAtWar.DevTools.PipelineSteps.Build;

public class CompileLocalizationStep(BuildSettings settings, IServiceProvider serviceProvider) : PipelineStep(serviceProvider)
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
        if (!settings.CleanBuild && !updateChecker.RequiresUpdate(datFilePath, new List<string> { file }))
        {
            _logger?.LogDebug($"DAT data '{datFileName}' is already up to date. Skipping build.");
            return;
        }

        _logger?.LogInformation($"Writing DAT data '{datFileName}'...");

        var locFileService = new LocalizationFileService(Services, settings.WarnAsError);

        var localizationFile = locFileService.ReadLocalizationFile(file);

        if (localizationFile.Language != locFileService.LanguageNameFromFileName(datFileName.AsSpan()))
            throw new InvalidOperationException();

        locFileService.CompileLocalizationFile(localizationFile, datFilePath, true);
        
        _logger?.LogInformation($"Finished writing DAT data for language {localizationFile.Language}");
    }
}