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
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using RepublicAtWar.DevLauncher.Utilities;

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

        ManuallyChooseDiffedText(englishDiff.ChangedEntries);

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
                {
                    changedEntries.Add((new DatStringEntry(entry.Key, entry.Crc32, currentValue), entry.Value));
                }
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

    private void ManuallyChooseDiffedText(ICollection<(DatStringEntry baseEntry, string changedValue)> diffedEntries)
    {
        if (diffedEntries.Count == 0)
            return;

        using var diffFile = new TempDiffEntriesFile("Data/Text/diffEntries.diff", _fileSystem);
        
        Console.Clear();

        var copy = diffedEntries.ToList();
        var total = copy.Count;
        var counter = 0;

        foreach (var valueTuple in copy)
        {
            var oldText = valueTuple.baseEntry.Value;
            var newText = valueTuple.changedValue;
            var key = valueTuple.baseEntry.Key;

            if (string.IsNullOrEmpty(oldText) ^ string.IsNullOrEmpty(newText))
            {
                counter++;
                continue;
            }

            if (diffFile.TryGet(key, out var action))
            {
                if (action == TempDiffEntriesFile.DiffAction.Ignore)
                    diffedEntries.Remove(valueTuple);
                counter++;
                continue;
            }

            Console.WriteLine($"Progress: ({counter++}/{total})");
            Console.WriteLine();
            Console.WriteLine($"{valueTuple.baseEntry.Key}:");
            CompuMaster.Text.Diffs.DumpDiffToConsole(newText, oldText, true);
            Console.WriteLine();


            var keepEntry = ConsoleUtilities.UserQuestionOnSameLine("Keep? [y/n]: ", (string input, out bool keep) =>
            {
                if (input.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    keep = true;
                    return true;
                }
                if (input.Equals("n", StringComparison.OrdinalIgnoreCase))
                {
                    keep = false;
                    return true;
                }

                keep = false;
                return false;
            });

            diffFile.Add(key, keepEntry ? TempDiffEntriesFile.DiffAction.Keep : TempDiffEntriesFile.DiffAction.Ignore);
            if (!keepEntry)
                diffedEntries.Remove(valueTuple);

            Console.Clear();
        }
    }

    private class TempDiffEntriesFile : IDisposable
    {
        public enum DiffAction
        {
            Keep,
            Ignore
        }

        private readonly HashSet<string> _keeps = new();
        private readonly HashSet<string> _ignores = new();
        private readonly StreamWriter _writer;

        public TempDiffEntriesFile(string filePath, IFileSystem fileSystem)
        {
            if (!fileSystem.File.Exists(filePath))
                fileSystem.File.Create(filePath).Dispose();

            // Read existing entries
            foreach (var line in fileSystem.File.ReadAllLines(filePath))
            {
                if (line.Length< 2) continue;

                var actionChar = line[0];
                var entry = line[1..];

                if (actionChar == '+')
                    _keeps.Add(entry);
                else if (actionChar == '-')
                    _ignores.Add(entry);
            }

            // Open the file in append mode for writing new entries
            _writer = new StreamWriter(filePath, append: true) { AutoFlush = true };
        }

        public bool Add(string entry, DiffAction result)
        {
            if (_keeps.Contains(entry) || _ignores.Contains(entry))
                return false;

            switch (result)
            {
                case DiffAction.Keep:
                    _keeps.Add(entry);
                    _writer.WriteLine("+" + entry);
                    break;
                case DiffAction.Ignore:
                    _ignores.Add(entry);
                    _writer.WriteLine("-" + entry);
                    break;
            }

            return true;
        }

        public bool TryGet(string entry, out DiffAction result)
        {
            if (_keeps.Contains(entry))
            {
                result = DiffAction.Keep;
                return true;
            }

            if (_ignores.Contains(entry))
            {
                result = DiffAction.Ignore;
                return true;
            }

            result = default;
            return false;
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
}
}