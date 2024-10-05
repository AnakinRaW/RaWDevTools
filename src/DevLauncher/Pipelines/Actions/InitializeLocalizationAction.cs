using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Files.DAT.Data;
using PG.StarWarsGame.Files.DAT.Files;
using PG.StarWarsGame.Files.DAT.Services;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using RepublicAtWar.DevTools.Localization;
using RepublicAtWar.DevTools.Services;
using RepublicAtWar.DevTools.Steps;

namespace RepublicAtWar.DevLauncher.Pipelines.Actions;

internal class InitializeLocalizationAction(IServiceProvider serviceProvider) : SingleActionPipeline(serviceProvider, true)
{
    private const string EnglishDAT = "MasterTextFile_English.DAT";

    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    private readonly LocalizationFileService _localizationFileService = new(serviceProvider, true);
    private readonly IDatFileService _datFileService = serviceProvider.GetRequiredService<IDatFileService>();
    private readonly IDatModelService _modelService = serviceProvider.GetRequiredService<IDatModelService>();

    protected override void RunAction(CancellationToken cancellationToken)
    {
        Logger?.LogInformation($"Processing data '{EnglishDAT}'");

        var englishMtfPath = _fileSystem.Path.Combine("Data\\Text", EnglishDAT);
        var englishMasterText = _datFileService.LoadAs(englishMtfPath, DatFileType.OrderedByCrc32).Content;

        var datFiles = _fileSystem.Directory.GetFiles("Data\\Text", "MasterTextFile_*.dat");

        foreach (var datFile in datFiles)
        {
            Logger?.LogInformation($"Processing data '{_fileSystem.Path.GetFileName(datFile)}'");

            var language = _localizationFileService.LanguageNameFromFileName(datFile.AsSpan());

            var datModel = LoadAndRemoveDuplicates(datFile);
            var localizationFile = CreateLocalizationFileModelFromDatAndReference(datModel, englishMasterText, language);
            
            CrossValidate(datFile, localizationFile);

            var localizationFilePath = _fileSystem.Path.ChangeExtension(datFile, "txt");
            using var fileStream = _fileSystem.FileStream.New(localizationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            
            _localizationFileService.WriteLocalizationFile(fileStream, localizationFile);
        }
    }

    private LocalizationFile CreateLocalizationFileModelFromDatAndReference(IDatModel datModel, IDatModel referenceModel, LanguageType language)
    {
        var normalEntries = new List<DatStringEntry>();
        var entriesWithMissingValue = new List<DatStringEntry>();
        var englishValueEntries = new List<DatStringEntry>();

        foreach (var entry in datModel)
        {
            var englishEntry = referenceModel.EntriesWithCrc(entry.Crc32).First();

            if (entry.Value == string.Empty)
            {
                if (englishEntry.Value == string.Empty)
                    normalEntries.Add(entry);
                else
                    entriesWithMissingValue.Add(englishEntry);
            }
            else
            {
                if (entry.Value.Equals(englishEntry.Value) && !string.IsNullOrWhiteSpace(entry.Value))
                    englishValueEntries.Add(entry);
                else
                    normalEntries.Add(entry);
            }
        }
        
        var entries = normalEntries
            .Concat(englishValueEntries.Union(entriesWithMissingValue)).OrderBy(e => e.Key)
            .Select(e => new LocalizationEntry(e.Key, e.Value))
            .ToList();

        return new LocalizationFile(language, entries);
    }

    private void CrossValidate(string datFilePath, LocalizationFile localizationFile)
    {
        var builder = new EmpireAtWarMasterTextBuilder(false, ServiceProvider);

        foreach (var entry in localizationFile.Entries)
        {
            var result = builder.AddEntry(entry.Key, entry.Value);
            if (!result.Added)
                LogOrThrow($"Unable to add KEY '{entry.Key}' to the DAT data.");
        }

        const string checkDat = "Data\\Text\\Check.dat";

        builder.Build(new DatFileInformation { FilePath = _fileSystem.Path.GetFullPath(checkDat) }, true);

        var datService = ServiceProvider.GetRequiredService<IDatFileService>();

        var org = datService.LoadAs(datFilePath, DatFileType.OrderedByCrc32).Content;
        var other = datService.LoadAs(checkDat, DatFileType.OrderedByCrc32).Content;


        var hashingService = ServiceProvider.GetRequiredService<IHashingService>();
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

    private IDatModel LoadAndRemoveDuplicates(string datFilePath)
    {
        var masterText = _datFileService.LoadAs(datFilePath, DatFileType.OrderedByCrc32).Content;

        var duplicates = _modelService.GetDuplicateEntries(masterText);

        if (duplicates.Any())
        {
            var sb = new StringBuilder($"MasterTextFile '{datFilePath}' contains duplicates:");
            foreach (var entry in duplicates)
                sb.AppendLine($"Duplicate Key: '{entry.Key}'");

            LogOrThrow(sb.ToString());
            masterText = _modelService.RemoveDuplicates(masterText);
        }
        return masterText;
    }
}