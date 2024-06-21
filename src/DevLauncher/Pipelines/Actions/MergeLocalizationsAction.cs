using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using PG.StarWarsGame.Engine.Language;
using RepublicAtWar.DevTools.Localization;
using RepublicAtWar.DevTools.Services;
using RepublicAtWar.DevTools.Steps;
using RepublicAtWar.DevTools.Utilities;

namespace RepublicAtWar.DevLauncher.Pipelines.Actions;

internal class MergeLocalizationsAction(IServiceProvider serviceProvider) : SingleActionPipeline(serviceProvider, true)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly LocalizationFileService _localizationFileService = new(serviceProvider, true);

    protected override void RunAction(CancellationToken cancellationToken)
    {
        foreach (var diffFile in _fileSystem.Directory.EnumerateFiles("Data\\Text", "Diff_MasterTextFile_*.txt"))
        {
            var locFile = diffFile.Replace("Diff_", "");
            if (!_fileSystem.File.Exists(locFile))
            {
                LogOrThrow($"Unable to find localization data '{locFile}' for DIFF data '{diffFile}'");
                continue;
            }

            var currentDiff = _localizationFileService.ReadLocalizationFile(diffFile);

            if (currentDiff.Language == LanguageType.English)
                LogOrThrow("ENGLISH language should not use diff files.");

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
    }
}