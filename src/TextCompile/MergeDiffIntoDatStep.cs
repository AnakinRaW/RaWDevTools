using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RepublicAtWar.DevTools.PipelineSteps.Settings;
using RepublicAtWar.DevTools.Services;

namespace RepublicAtWar.TextCompile;

internal class MergeDiffIntoDatStep(IServiceProvider serviceProvider, BuildSettings buildSettings) : PipelineStep(serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(MergeDiffIntoDatStep));

    protected override void RunCore(CancellationToken token)
    {
        var diffFiles = _fileSystem.Directory.EnumerateFiles("Data\\Text", "Diff_MasterTextFile_*.txt");

        foreach (var localizationFile in diffFiles)
            MergeDiffIntoDat(localizationFile);
    }

    private void MergeDiffIntoDat(string file)
    {
        var datFilePath = _fileSystem.Path.ChangeExtension(file.Replace("Diff_", ""), "dat");

        if (!_fileSystem.File.Exists(datFilePath))
            throw new FileNotFoundException("Unable to find DAT file", datFilePath);
        
        var localizationService = new LocalizationFileService(Services, true);
        localizationService.MergeDiffsIntoDatFiles(file, datFilePath);
    }
}