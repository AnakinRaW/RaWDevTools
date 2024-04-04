using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Files.DAT.Data;
using PG.StarWarsGame.Files.DAT.Files;
using PG.StarWarsGame.Files.DAT.Services;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using RepublicAtWar.DevLauncher.Localization;

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

    private LocalizationFileWriter LocFileWriter => serviceProvider.GetRequiredService<LocalizationFileWriter>();

    public void InitializeFromDatFiles()
    {
        _logger?.LogInformation($"Processing file '{EnglishDAT}'");
        
        var englishMtfPath = _fileSystem.Path.Combine("Data\\Text", EnglishDAT);
        var englishMasterText = LocFileWriter.DatToLocalizationFile(englishMtfPath);
        var englishTextFile = _fileSystem.Path.ChangeExtension(englishMtfPath, "txt");

        CrossValidate(englishMtfPath, englishTextFile);

        var datFiles = _fileSystem.Directory.GetFiles("Data\\Text", "MasterTextFile_*.dat");

        foreach (var datFile in datFiles)
        {
            var fileName = _fileSystem.Path.GetFileName(datFile);
            if (fileName.ToUpperInvariant().Equals(EnglishDAT.ToUpperInvariant()))
                continue;

            _logger?.LogInformation($"Processing file '{fileName}'");

            LocFileWriter.InitializeFromDatAndEnglishReference(datFile, englishMasterText);

            var localizationFilePath = _fileSystem.Path.ChangeExtension(datFile, "txt");

            CrossValidate(datFile, localizationFilePath);
        }
    }

    public void UpdateNonEnglishFiles()
    {
        var englishMtfPath = _fileSystem.Path.Combine("Data\\Text", EnglishText);
        var englishMasterText = CreateModelFromLocalizationFile(englishMtfPath);

        var foreignLangFiles = _fileSystem.Directory.GetFiles("Data\\Text", "MasterTextFile_*.txt");

        foreach (var langFile in foreignLangFiles)
        {
            var fileName = _fileSystem.Path.GetFileName(langFile);
            if (fileName.ToUpperInvariant().Equals(EnglishText.ToUpperInvariant()))
                continue;

            _logger?.LogInformation($"Merging Missing KEYs from English into file '{fileName}'");

            var masterText = CreateModelFromLocalizationFile(langFile);

            var missingKeys = _modelService.GetMissingKeysFromBase(englishMasterText, masterText);

            var missingEntries = new List<DatStringEntry>(missingKeys.Count);
            foreach (var missingKey in missingKeys)
                missingEntries.Add(englishMasterText.FirstEntryWithKey(missingKey));

            LocFileWriter.AppendEntries(langFile, missingEntries);
        }
    }

    private IDatModel CreateModelFromLocalizationFile(string file)
    {
        var builder = new EmpireAtWarMasterTextBuilder(false, _serviceProvider);

        var textFileModel = new LocalizationFileReader(false, serviceProvider).ReadFile(file);

        foreach (var entry in textFileModel.Entries)
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
}