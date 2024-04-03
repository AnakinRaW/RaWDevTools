using System;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Files.DAT.Data;
using PG.StarWarsGame.Files.DAT.Files;
using PG.StarWarsGame.Files.DAT.Services;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using RepublicAtWar.DevLauncher.Localization;

namespace RepublicAtWar.DevLauncher.Services;


// This service is used only once, to initialize all localization files from existing .DAT files
internal class LocalizationInitializer(IServiceProvider serviceProvider)
{
    private const string MasterTextFileEnglish = "MasterTextFile_English.DAT";

    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ILogger? _logger = serviceProvider.GetRequiredService<ILoggerFactory>()?.CreateLogger(typeof(LocalizationInitializer));

    public void Run()
    {
        var englishModel = CreateEnglishLocalizationFile();

        var datFiles = _fileSystem.Directory.GetFiles("Data\\Text", "MasterTextFile_*.dat");

        foreach (var datFile in datFiles)
        {
            var fileName = _fileSystem.Path.GetFileName(datFile);
            if (fileName.ToUpperInvariant().Equals(MasterTextFileEnglish.ToUpperInvariant()))
                continue;

            _logger?.LogInformation($"Processing file '{fileName}'");

            new LocalizationFileWriter(_serviceProvider).InitializeFromDatAndEnglishReference(datFile, englishModel);

            var localizationFilePath = _fileSystem.Path.ChangeExtension(datFile, "txt");

            CrossValidate(datFile, localizationFilePath);
        }
    }


    private IDatModel CreateEnglishLocalizationFile()
    {
        _logger?.LogInformation($"Processing file '{MasterTextFileEnglish}'");

        var englishMtfPath = _fileSystem.Path.Combine("Data\\Text", MasterTextFileEnglish);

        var englishMasterText = new LocalizationFileWriter(_serviceProvider).DatToLocalizationFile(englishMtfPath);
        var localizationFilePath = _fileSystem.Path.ChangeExtension(englishMtfPath, "txt");

        CrossValidate(englishMtfPath, localizationFilePath);

        return englishMasterText;
    }


    private void CrossValidate(string datFilePath, string localizationFilePath)
    {
        using var locFileFs = _fileSystem.FileStream.New(localizationFilePath, FileMode.Open);
        var reader = new LocalizationFileReaderReader(false, _serviceProvider);

        var locFile = reader.FromStream(locFileFs);

        var builder = new EmpireAtWarMasterTextFileBuilder(false, _serviceProvider);

        foreach (var entry in locFile.Entries)
        {
            var result = builder.AddEntry(entry.Key, entry.Value);
            if (!result.Added)
                _logger?.LogWarning($"Unable to add KEY '{entry.Key}' to the DAT file.");
        }

        const string checkDat = "Data\\Text\\Check.dat";

        builder.Build(new DatFileInformation { FilePath = _fileSystem.Path.GetFullPath(checkDat) }, true);

        var datService = _serviceProvider.GetRequiredService<IDatFileService>();

        var org = datService.LoadAs(datFilePath, DatFileType.OrderedByCrc32).Content;
        var other = datService.LoadAs(checkDat, DatFileType.OrderedByCrc32).Content;

        if (org.Count != other.Count)
            throw new InvalidOperationException();

        for (var i = 0; i < org.Count; i++)
        {
            var oe = org[i];
            var ne = other[i];
            if (!oe.Equals(ne)) 
                _logger?.LogWarning($"Original Entry '{oe}' and new Entry '{ne}' are not equal.");
        }

        _fileSystem.File.Delete(checkDat);
    }
}