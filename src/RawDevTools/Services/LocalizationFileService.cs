using System;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Files.DAT.Data;
using PG.StarWarsGame.Files.DAT.Files;
using PG.StarWarsGame.Files.DAT.Services;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using RepublicAtWar.DevTools.Localization;

namespace RepublicAtWar.DevTools.Services;

public class LocalizationFileService(IServiceProvider serviceProvider, bool warningAsError = false)
{
    private const string EnglishDAT = "MasterTextFile_English.DAT";
    private const string EnglishText = "MasterTextFile_English.txt";

    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly IDatFileService _datFileService = serviceProvider.GetRequiredService<IDatFileService>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(LocalizationFileService));

    private LocalizationFileWriter LocalizationFileWriter => serviceProvider.GetRequiredService<LocalizationFileWriter>();

    public void MergeDiffsIntoDatFiles(string diffFile, string datFile)
    {
        if (!_fileSystem.File.Exists(diffFile))
            throw new FileNotFoundException("Unable to find Diff txt file", diffFile);
        if (!_fileSystem.File.Exists(datFile))
            throw new FileNotFoundException("Unable to find DAT file", datFile);

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

            // TODO: Currently the lib does not encode before when using RemoveAllKeys(), thus, adding and then removing the entry is safer.
            if (diffEntry.IsDeletedValue() && addResult.Added)
                builder.Remove(addResult.AddedEntry.Value);
        }

        builder.Build(datModel.FileInformation, true);
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

    private void LogOrThrow(string message)
    {
        if (warningAsError)
            throw new InvalidOperationException(message);
        _logger?.LogWarning(message);
    }
}