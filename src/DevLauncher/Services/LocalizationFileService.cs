using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(LocalizationFileService));

    private DevToolsOptionBase Options { get; } = options;

    private LocalizationFileWriter LocalizationFileWriter => serviceProvider.GetRequiredService<LocalizationFileWriter>();

    public void InitializeFromDatFiles()
    {
        _logger?.LogInformation($"Processing file '{EnglishDAT}'");
        
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

            _logger?.LogInformation($"Processing file '{fileName}'");

            LocalizationFileWriter.InitializeFromDatAndEnglishReference(datFile, englishMasterText);

            var localizationFilePath = _fileSystem.Path.ChangeExtension(datFile, "txt");

            CrossValidate(datFile, localizationFilePath);
        }
    }

    /// <summary>
    /// Creates a difference between the current english master text file and the latest stable version master text file
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
            .GetLatestStableVersion(englishTextPath, "7205f0ce23ee31f133046e3c905f74f291325143");

        if (oldEnglishFileStream is null)
            throw new InvalidOperationException("Unable to find an oldest reference english master text file.");
        
        var oldEnglish = CreateModelFromLocalizationFile(
            new LocalizationFileReader(false, serviceProvider).FromStream(oldEnglishFileStream));

        return oldEnglish;
    }


    public void CreateForeignDiffFiles()
    {
        var englishDiff = CreateEnglishDiff();

        var foreignLangFiles = _fileSystem.Directory.GetFiles("Data\\Text", "MasterTextFile_*.txt");

        foreach (var langFile in foreignLangFiles)
        {
            var fileName = _fileSystem.Path.GetFileName(langFile);
            if (fileName.ToUpperInvariant().Equals(EnglishText.ToUpperInvariant()))
                continue;

            _logger?.LogInformation($"Creating Diff for file '{fileName}'");

            var locFile = ReadLocalizationFile(langFile);
            var masterText = CreateModelFromLocalizationFile(locFile);

            var newEntries = new List<DatStringEntry>();
            var changedEntries = new List<(DatStringEntry newEntry, string currentValue)>();
            var keysToDelete = new HashSet<string>();

            foreach (var newEntry in englishDiff.NewEntries)
            {
                if (!masterText.ContainsKey(newEntry.Crc32))
                    newEntries.Add(newEntry);
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

    public void MergeDiffsInfoFiles()
    {
        foreach (var diffFile in _fileSystem.Directory.EnumerateFiles("Data\\Text", "Diff_MasterTextFile_*.txt"))
        {
            var locFile = diffFile.Replace("Diff_", "");
            if (!_fileSystem.File.Exists(locFile))
            {
                LogOrThrow($"Unable to find localization file '{locFile}' for DIFF file '{diffFile}'");
                continue;
            }

            var masterTextLoc = ReadLocalizationFile(locFile);

            if (masterTextLoc.Language.ToUpperInvariant() == "ENGLISH") 
                LogOrThrow("ENGLISH language should not use diff files.");

            var currentDiff = ReadLocalizationFile(diffFile);

            var entries = new KeyValuePairList<string, LocalizationEntry>();

            foreach (var entry in masterTextLoc.Entries.Concat(currentDiff.Entries))
            {
                if (entry.IsDeletedValue())
                    continue;
                if (entries.ContainsKey(entry.Key, out _))
                    entries.Replace(entry.Key, entry);
                else
                    entries.Add(entry.Key, entry);
            }

            using var fs = _fileSystem.FileStream.New(locFile, FileMode.Create);
            LocalizationFileWriter.WriteFile(fs, new LocalizationFile(masterTextLoc.Language, entries.GetValueList()));
        }
    }

    private LocalizationFile ReadLocalizationFile(string path)
    { 
        return new LocalizationFileReader(false, serviceProvider).ReadFile(path);
    }

    private IDatModel CreateModelFromLocalizationFile(LocalizationFile file)
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
        using var locFileFs = _fileSystem.FileStream.New(localizationFilePath, FileMode.Open);
        var reader = new LocalizationFileReader(false, _serviceProvider);

        var locFile = reader.FromStream(locFileFs);

        var builder = new EmpireAtWarMasterTextBuilder(false, _serviceProvider);

        foreach (var entry in locFile.Entries)
        {
            var result = builder.AddEntry(entry.Key, entry.Value);
            if (!result.Added)
                LogOrThrow($"Unable to add KEY '{entry.Key}' to the DAT file.");
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

    public IDatModel LoadLocalization(string localizationFileName)
    {
        return CreateModelFromLocalizationFile(
            ReadLocalizationFile(_fileSystem.Path.Combine("Data/Text", localizationFileName)));
    }
}


public class KeyValuePairList<TKey, TValue> where TKey : notnull
{
    private readonly HashSet<TKey> _keys = new();
    private readonly List<(TKey key, TValue value)> _items = new();
    
    public bool ContainsKey(TKey key, [NotNullWhen(true)] out TValue? firstOrDefault)
    {
        firstOrDefault = default;
        if (!_keys.Contains(key))
            return false;
        var firstEntry = _items.First(i => EqualityComparer<TKey>.Default.Equals(i.key, key));
        firstOrDefault = firstEntry.value!;
        return true;
    }

    public void Add(TKey key, TValue value)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));
        _items.Add((key, value));
        _keys.Add(key);
    }

    public void Replace(TKey key, TValue value)
    {
        var index = _items.FindIndex(i => EqualityComparer<TKey>.Default.Equals(i.key, key));
        _items[index] = (key, value);
    }

    public void Clear()
    {
        _items.Clear();
        _keys.Clear();
    }

    public bool Remove(TKey key, TValue value)
    {
        var result = _items.Remove((key, value));
        if (!_items.Any(x => EqualityComparer<TKey>.Default.Equals(x.key, key)))
            _keys.Remove(key);
        return result;
    }

    public bool RemoveAll(TKey key)
    {
        var result = _items.RemoveAll(i => EqualityComparer<TKey>.Default.Equals(i.key, key)) > 0;
        _keys.Remove(key);
        return result;
    }

    public IList<TValue> GetValueList()
    {
        return _items.Select(i => i.value).ToList();
    }
}