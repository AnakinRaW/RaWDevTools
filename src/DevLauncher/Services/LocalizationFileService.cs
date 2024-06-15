using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using AnakinRaW.CommonUtilities.FileSystem.Normalization;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Files.DAT.Data;
using PG.StarWarsGame.Files.DAT.Files;
using PG.StarWarsGame.Files.DAT.Services;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using RepublicAtWar.DevLauncher.Localization;
using RepublicAtWar.DevLauncher.Options;

namespace RepublicAtWar.DevLauncher.Services;

internal class LocalizationFileService(DevToolsOptionBase options, IServiceProvider serviceProvider)
{
    private const string EnglishDAT = "MasterTextFile_English.DAT";
    private const string EnglishText = "MasterTextFile_English.txt";

    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly IDatModelService _modelService= serviceProvider.GetRequiredService<IDatModelService>();
    private readonly IDatFileService _datFileService = serviceProvider.GetRequiredService<IDatFileService>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(LocalizationFileService));

    private DevToolsOptionBase Options { get; } = options;

    private LocalizationFileWriter LocalizationFileWriter => serviceProvider.GetRequiredService<LocalizationFileWriter>();

    public void InitializeFromDatFiles()
    {
        _logger?.LogInformation($"Processing data '{EnglishDAT}'");
        
        var englishMtfPath = _fileSystem.Path.Combine("Data\\Text", EnglishDAT);
        var englishMasterText = LocalizationFileWriter.DatToLocalizationFile(englishMtfPath);
        var englishTextFile = _fileSystem.Path.ChangeExtension(englishMtfPath, "txt");

        CrossValidate(englishMtfPath, englishTextFile);

        var datFiles = _fileSystem.Directory.GetFiles("Data\\Text", "MasterTextFile_*.dat");

        foreach (var datFile in datFiles)
        {
            var fileName = _fileSystem.Path.GetFileName(datFile);
            if (fileName.ToUpperInvariant().Equals(EnglishDAT.ToUpperInvariant()))
                continue;

            _logger?.LogInformation($"Processing data '{fileName}'");

            LocalizationFileWriter.InitializeFromDatAndEnglishReference(datFile, englishMasterText);

            var localizationFilePath = _fileSystem.Path.ChangeExtension(datFile, "txt");

            CrossValidate(datFile, localizationFilePath);
        }
    }

    /// <summary>
    /// Creates a difference between the current english master text data and the latest stable version master text data
    /// </summary>
    private MasterTextDifference CreateEnglishDiff()
    {
        var currentEnglish = CreateModelFromLocalizationFile(
            ReadLocalizationFile(_fileSystem.Path.Combine("Data\\Text", EnglishText)));

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

       var gitService = _serviceProvider.GetRequiredService<GitService>();

       if (gitService.CurrentBranch != "master")
           LogOrThrow("Current branch is not master!");

        // This commit introduced the localization TXT files.
        using var oldEnglishFileStream = _serviceProvider.GetRequiredService<GitService>()
            .GetLatestStableVersion(englishTextPath, "f56067b5010d8ae3a3687532493d1856e2287b48");

        if (oldEnglishFileStream is null)
            throw new InvalidOperationException("Unable to find an oldest reference english master text data.");

        using var reader = new LocalizationFileReader(oldEnglishFileStream, false, _serviceProvider);
        var oldEnglish = CreateModelFromLocalizationFile(reader.Read());
        return oldEnglish;
    }


    public void CreateForeignDiffFiles()
    {
        var englishDiff = CreateEnglishDiff();
        var englishFile =
            CreateModelFromLocalizationFile(ReadLocalizationFile(_fileSystem.Path.Combine("Data\\Text", EnglishText)));

        var foreignLangFiles = _fileSystem.Directory.GetFiles("Data\\Text", "MasterTextFile_*.txt");

        foreach (var langFile in foreignLangFiles)
        {
            var fileName = _fileSystem.Path.GetFileName(langFile);
            if (fileName.ToUpperInvariant().Equals(EnglishText.ToUpperInvariant()))
                continue;

            _logger?.LogInformation($"Creating Diff for data '{fileName}'");

            var locFile = ReadLocalizationFile(langFile);
            var masterText = CreateModelFromLocalizationFile(locFile);

            var newEntries = new List<DatStringEntry>();
            var changedEntries = new List<(DatStringEntry newEntry, string currentValue)>();
            var keysToDelete = new HashSet<string>();

            foreach (var missingEntry in _modelService.GetMissingKeysFromBase(englishFile, masterText))
            {
                newEntries.Add(englishFile.FirstEntryWithKey(missingEntry));
            }

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
                    changedEntries.Add((entry, currentValue));
            }

            var diff = new MasterTextDifference(newEntries, changedEntries, keysToDelete);

            var diffFileName = $"Diff_MasterTextFile_{locFile.Language}.txt";
            using var fs = _fileSystem.FileStream.New("Data\\Text\\" + diffFileName, FileMode.Create);
            LocalizationFileWriter.CreateDiffFile(fs, locFile.Language, diff);
        }
    }


    public void MergeDiffsIntoDatFiles()
    {
        foreach (var diffFile in _fileSystem.Directory.EnumerateFiles("Data\\Text", "Diff_MasterTextFile_*.txt"))
        {
            var datFile = _fileSystem.Path.ChangeExtension(diffFile.Replace("Diff_", ""), "dat");
            if (!_fileSystem.File.Exists(datFile))
            {
                LogOrThrow($"Unable to find DAT  file '{datFile}' for DIFF data '{diffFile}'");
                continue;
            }

            var currentDiff = ReadLocalizationFile(diffFile);
            
            if (currentDiff.Language.ToUpperInvariant() == "ENGLISH")
                LogOrThrow("ENGLISH language should not use diff files.");

            using var datModel = _datFileService.LoadAs(datFile, DatFileType.OrderedByCrc32);

            var builder = new EmpireAtWarMasterTextBuilder(true, _serviceProvider);

            foreach (var entry in datModel.Content) 
                builder.AddEntry(entry.Key, entry.Value);

            foreach (var diffEntry in currentDiff.Entries)
            {
                var addResult = builder.AddEntry(diffEntry.Key, diffEntry.Key);
                if (!addResult.Added)
                    LogOrThrow($"Unable to add KEY '{diffEntry.Key}' to the DAT model.");

                if (diffEntry.IsDeletedValue() && addResult.Added) 
                    builder.Remove(addResult.AddedEntry.Value);

            }
        }
    }

    public void MergeDiffsIntoTextFiles()
    {
        foreach (var diffFile in _fileSystem.Directory.EnumerateFiles("Data\\Text", "Diff_MasterTextFile_*.txt"))
        {
            var locFile = diffFile.Replace("Diff_", "");
            if (!_fileSystem.File.Exists(locFile))
            {
                LogOrThrow($"Unable to find localization data '{locFile}' for DIFF data '{diffFile}'");
                continue;
            }

            var currentDiff = ReadLocalizationFile(diffFile);
            
            if (currentDiff.Language.ToUpperInvariant() == "ENGLISH") 
                LogOrThrow("ENGLISH language should not use diff files.");

            var masterTextLoc = ReadLocalizationFile(locFile);

            if (!string.Equals(currentDiff.Language, masterTextLoc.Language, StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidOperationException($"Diff file is using a different language than its MasterTextFile.txt: {currentDiff.Language} <--> {masterTextLoc.Language}");


            var maxItemCount = masterTextLoc.Entries.Count + currentDiff.Entries.Count;
            var entries = new OrderedDictionary<string, LocalizationEntry>(maxItemCount);

            foreach (var entry in masterTextLoc.Entries.Concat(currentDiff.Entries))
            {
                if (entry.IsDeletedValue())
                    continue;
                entries.AddOrReplace(entry.Key, entry);
            }

            using var fs = _fileSystem.FileStream.New(locFile, FileMode.Create);
            LocalizationFileWriter.WriteFile(fs, new LocalizationFile(masterTextLoc.Language, entries.GetValues()));
        }
    }

    public LocalizationFile ReadLocalizationFile(string path)
    {
        using var reader = new LocalizationFileReader(path, false, serviceProvider);
        return reader.Read();
    }

    public IDatModel CreateModelFromLocalizationFile(LocalizationFile file)
    {
        var builder = new EmpireAtWarMasterTextBuilder(false, _serviceProvider);
        
        foreach (var entry in file.Entries)
        {
            var result = builder.AddEntry(entry.Key, entry.Value);
            if (!result.Added)
                LogOrThrow($"Unable to add KEY '{entry.Key}' to the DAT model.");
        }

        return builder.BuildModel();
    }


    private void CrossValidate(string datFilePath, string localizationFilePath)
    {
        var locFile = ReadLocalizationFile(localizationFilePath);
       
        var builder = new EmpireAtWarMasterTextBuilder(false, _serviceProvider);

        foreach (var entry in locFile.Entries)
        {
            var result = builder.AddEntry(entry.Key, entry.Value);
            if (!result.Added)
                LogOrThrow($"Unable to add KEY '{entry.Key}' to the DAT data.");
        }

        const string checkDat = "Data\\Text\\Check.dat";

        builder.Build(new DatFileInformation { FilePath = _fileSystem.Path.GetFullPath(checkDat) }, true);

        var datService = _serviceProvider.GetRequiredService<IDatFileService>();

        var org = datService.LoadAs(datFilePath, DatFileType.OrderedByCrc32).Content;
        var other = datService.LoadAs(checkDat, DatFileType.OrderedByCrc32).Content;


        var hashingService = _serviceProvider.GetRequiredService<IHashingService>();
        var orgHash = hashingService.GetHash(_fileSystem.FileInfo.New(datFilePath), HashTypeKey.SHA256);
        var newHash = hashingService.GetHash(_fileSystem.FileInfo.New(checkDat), HashTypeKey.SHA256);

        if (!orgHash.SequenceEqual(newHash))
            LogOrThrow("Original Entry original and cross-check dat files are not equal.");

        if (org.Count != other.Count)
            throw new InvalidOperationException();

        for (var i = 0; i < org.Count; i++)
        {
            var oe = org[i];
            var ne = other[i];
            if (!oe.Equals(ne) && oe.Value != string.Empty)
            {
                LogOrThrow($"Original Entry '{oe}' and new Entry '{ne}' are not equal.");
            }
        }

        _fileSystem.File.Delete(checkDat);
    }

    private void LogOrThrow(string message)
    {
        if (Options.WarnAsError)
            throw new InvalidOperationException(message);
        _logger?.LogWarning(message);
    }
}


internal class OrderedDictionary<TKey, TValue>(int capacity) where TKey : notnull
{
    private readonly Dictionary<TKey, int> _keys = new(capacity, EqualityComparer<TKey>.Default);
    private readonly List<TValue> _items = new(capacity);

    public bool AddOrReplace(TKey key, TValue value) 
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (!_keys.TryGetValue(key, out var index))
        {
            var newIndex = _items.Count;
            _items.Add(value);
            _keys.Add(key, newIndex);
            return false;
        }

        _items[index] = value;
        return true;
    }

    public IList<TValue> GetValues()
    {
        return _items.ToList();
    }
}