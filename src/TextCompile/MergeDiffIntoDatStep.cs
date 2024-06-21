using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Engine.Language;
using RepublicAtWar.DevTools.Localization;
using RepublicAtWar.DevTools.Services;
using RepublicAtWar.DevTools.Steps.Settings;
using RepublicAtWar.DevTools.Utilities;

namespace RepublicAtWar.TextCompile;

internal class MergeDiffIntoDatStep(IServiceProvider serviceProvider, BuildSettings buildSettings) : PipelineStep(serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(MergeDiffIntoDatStep));

    private readonly LocalizationFileService _localizationFileService = new(serviceProvider, buildSettings.WarnAsError);

    protected override void RunCore(CancellationToken token)
    {
        var diffFiles = _fileSystem.Directory.EnumerateFiles("Data\\Text", "Diff_MasterTextFile_*.txt");
        var textFiles = _fileSystem.Directory.EnumerateFiles("Data\\Text", "MasterTextFile_*.txt");

        foreach (var localizationFile in diffFiles)
            MergeDiffIntoDatOrText(localizationFile);

        foreach (var textFile in textFiles)
        {
            var datFile = _fileSystem.Path.ChangeExtension(textFile, ".dat");
            var locFile = _localizationFileService.ReadLocalizationFile(textFile);
            _localizationFileService.CompileLocalizationFile(locFile, datFile , true);
        }
    }

    private void MergeDiffIntoDatOrText(string diffFile)
    {
        var locFile = diffFile.Replace("Diff_", "");
        if (_fileSystem.File.Exists(locFile))
        {
            var currentDiff = _localizationFileService.ReadLocalizationFile(diffFile);

            if (currentDiff.Language == LanguageType.English)
                throw new InvalidOperationException("ENGLISH language should not use diff files.");

            var masterTextLoc = _localizationFileService.ReadLocalizationFile(locFile);

            if (currentDiff.Language != masterTextLoc.Language)
                throw new InvalidOperationException($"Diff file is using a different language than its MasterTextFile.txt: {currentDiff.Language} <--> {masterTextLoc.Language}");


            var maxItemCount = masterTextLoc.Entries.Count + currentDiff.Entries.Count;
            var entries = new LinkedDictionary<string, LocalizationEntry>(maxItemCount);

            foreach (var entry in masterTextLoc.Entries.Concat(currentDiff.Entries))
            {
                if (entry.IsDeletedValue())
                    continue;
                entries.AddOrReplace(entry.Key, entry);
            }

            using var fs = _fileSystem.FileStream.New(locFile, FileMode.Create);
            _localizationFileService.WriteLocalizationFile(fs, new LocalizationFile(masterTextLoc.Language, entries.GetValues()));
        }
        else
        {
            var datFilePath = _fileSystem.Path.ChangeExtension(locFile, "dat");

            if (!_fileSystem.File.Exists(datFilePath))
                throw new FileNotFoundException("Unable to find DAT file", datFilePath);

            _localizationFileService.MergeDiffsIntoDatFiles(diffFile, datFilePath);
        }
    }
}