using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using AnakinRaW.CommonUtilities.FileSystem.Normalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Engine.Localization;
using PG.StarWarsGame.Files.DAT.Data;
using PG.StarWarsGame.Files.DAT.Services;
using RepublicAtWar.DevLauncher.Services;
using RepublicAtWar.DevTools.Localization;
using RepublicAtWar.DevTools.Services;
using RepublicAtWar.DevTools.Steps;

namespace RepublicAtWar.DevLauncher.Pipelines.Actions;

internal class CreateLocalizationDiffsAction(IServiceProvider serviceProvider) : SingleActionPipeline(serviceProvider, true)
{
    private const string EnglishText = "MasterTextFile_English.txt";

    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    private readonly LocalizationFileService _localizationFileService = new(serviceProvider, true);
    private readonly IDatFileService _datFileService = serviceProvider.GetRequiredService<IDatFileService>();
    private readonly IDatModelService _modelService = serviceProvider.GetRequiredService<IDatModelService>();

    protected override void RunAction(CancellationToken cancellationToken)
    {
        var englishDiff = CreateEnglishDiff();
        var englishFile = _localizationFileService.CreateModelFromLocalizationFile(_localizationFileService.ReadLocalizationFile(_fileSystem.Path.Combine("Data\\Text", EnglishText)));

        var foreignLangFiles = _fileSystem.Directory.GetFiles("Data\\Text", "MasterTextFile_*.txt");

        foreach (var langFile in foreignLangFiles)
        {
            var locFile = _localizationFileService.ReadLocalizationFile(langFile);
            if (locFile.Language == LanguageType.English)
                continue;

            Logger?.LogInformation($"Creating Diff for data '{langFile}'");

            var masterText = _localizationFileService.CreateModelFromLocalizationFile(locFile);

            var newEntries = new List<DatStringEntry>();
            var changedEntries = new List<(DatStringEntry newEntry, string currentValue)>();
            var keysToDelete = new HashSet<string>();

            foreach (var missingEntry in _modelService.GetMissingKeysFromBase(englishFile, masterText)) 
                newEntries.Add(englishFile.FirstEntryWithKey(missingEntry));

            foreach (var deletedKey in englishDiff.DeletedKeys)
            {
                if (masterText.ContainsKey(deletedKey))
                    keysToDelete.Add(deletedKey);
            }

            foreach (var (entry, _) in englishDiff.ChangedEntries)
            {
                if (!masterText.TryGetValue(entry.Crc32, out var currentValue))
                    newEntries.Add(entry);
                else
                    changedEntries.Add((new DatStringEntry(entry.Key, entry.Crc32, currentValue), entry.Value));
            }

            var diff = new MasterTextDifference(newEntries, changedEntries, keysToDelete);

            var diffFileName = $"Diff_MasterTextFile_{locFile.Language}.txt";
            using var fs = _fileSystem.FileStream.New("Data\\Text\\" + diffFileName, FileMode.Create);
            _localizationFileService.WriteDiffFile(fs, diff, locFile.Language);
        }
    }

    /// <summary>
    /// Creates a difference between the current english master text data and the latest stable version master text data
    /// </summary>
    private MasterTextDifference CreateEnglishDiff()
    {
        var currentEnglish = _localizationFileService.CreateModelFromLocalizationFile(
            _localizationFileService.ReadLocalizationFile(_fileSystem.Path.Combine("Data\\Text", EnglishText)));

        var oldEnglish = GetOldEnglishModel();

        var newEntries = new List<DatStringEntry>();
        var changedEntries = new List<(DatStringEntry newEntry, string oldValue)>();

        var deletedEntries = _modelService.GetMissingKeysFromBase(oldEnglish, currentEnglish);

        foreach (var entry in currentEnglish)
        {
            if (!oldEnglish.TryGetValue(entry.Crc32, out var oldValue))
                newEntries.Add(entry);
            else
            {
                if (!entry.Value.Equals(oldValue))
                    changedEntries.Add((entry, oldValue));
            }
        }

        return new MasterTextDifference(newEntries, changedEntries, deletedEntries);
    }

    private IDatModel GetOldEnglishModel()
    {
        var englishTextPath = PathNormalizer.Normalize(_fileSystem.Path.Combine("Data/Text", EnglishText),
            new PathNormalizeOptions
            {
                UnifyDirectorySeparators = true,
                UnifySeparatorKind = DirectorySeparatorKind.Linux
            });

        var gitService = ServiceProvider.GetRequiredService<GitService>();

        if (gitService.CurrentBranch != "master")
            LogOrThrow("Current branch is not master!");

        // This commit introduced the localization TXT files.
        using var oldEnglishFileStream = ServiceProvider.GetRequiredService<GitService>()
            .GetLatestStableVersion(englishTextPath, "f56067b5010d8ae3a3687532493d1856e2287b48");

        if (oldEnglishFileStream is null)
            throw new InvalidOperationException("Unable to find an oldest reference english master text data.");

        var locFile = _localizationFileService.ReadLocalizationFile(oldEnglishFileStream);
        
        return _localizationFileService.CreateModelFromLocalizationFile(locFile);
    }
}