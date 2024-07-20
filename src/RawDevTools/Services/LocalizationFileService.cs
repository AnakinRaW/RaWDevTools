using System;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Engine.Language;
using PG.StarWarsGame.Files.DAT.Data;
using PG.StarWarsGame.Files.DAT.Files;
using PG.StarWarsGame.Files.DAT.Services;
using PG.StarWarsGame.Files.DAT.Services.Builder;
using RepublicAtWar.DevTools.Localization;
using System.Text;
using PG.StarWarsGame.Engine;
#if NETSTANDARD2_0
using AnakinRaW.CommonUtilities.FileSystem;
#endif

namespace RepublicAtWar.DevTools.Services;

public class LocalizationFileService(IServiceProvider serviceProvider, bool warningAsError = false)
{
    private const string EnglishDAT = "MasterTextFile_English.DAT";
    private const string EnglishText = "MasterTextFile_English.txt";

    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly IDatFileService _datFileService = serviceProvider.GetRequiredService<IDatFileService>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(LocalizationFileService));

    private readonly IGameLanguageManager _languageManager = serviceProvider
        .GetRequiredService<IGameLanguageManagerProvider>().GetLanguageManager(GameEngineType.Foc);

    // TODO: Move to actual operation code
    public void MergeDiffsIntoDatFiles(string diffFile, string datFile)
    {
        if (!_fileSystem.File.Exists(diffFile))
            throw new FileNotFoundException("Unable to find Diff txt file", diffFile);
        if (!_fileSystem.File.Exists(datFile))
            throw new FileNotFoundException("Unable to find DAT file", datFile);

        var currentDiff = ReadLocalizationFile(diffFile);

        if (currentDiff.Language == LanguageType.English)
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
        var localizationFile = reader.Read();

        var fileName = _fileSystem.Path.GetFileName(path).AsSpan();
        var langName = LanguageNameFromFileName(fileName);
        if (localizationFile.Language != langName)
            LogOrThrow($"The file '{fileName.ToString()}' does not match the language content '{langName}'.");
        return localizationFile;
    }

    public LocalizationFile ReadLocalizationFile(Stream localizationFile)
    {
        using var reader = new LocalizationFileReader(localizationFile, warningAsError, serviceProvider);
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

    public void CompileLocalizationFile(LocalizationFile localizationFile, string datFile, bool overwrite)
    {
        using var builder = new EmpireAtWarMasterTextBuilder(false, _serviceProvider);

        foreach (var entry in localizationFile.Entries)
        {
            var result = builder.AddEntry(entry.Key, entry.Value);
            if (!result.Added)
                _logger?.LogWarning($"Unable to add KEY '{entry.Key}' to the DAT for language {localizationFile.Language}: {result.Message}");
        }

        builder.Build(new DatFileInformation { FilePath = _fileSystem.Path.GetFullPath(datFile) }, overwrite);
    }

    /// <summary>
    /// This works for MasterTextFile and Diff files
    /// </summary>
    public LanguageType LanguageNameFromFileName(ReadOnlySpan<char> path)
    {
        var fileNameWithoutExtension = _fileSystem.Path.GetFileNameWithoutExtension(path);
        var underScore = fileNameWithoutExtension.LastIndexOf('_');
        if (underScore == -1)
            throw new ArgumentException("Unable to get language from filename", nameof(path));

        var languageName = fileNameWithoutExtension.Slice(underScore + 1, fileNameWithoutExtension.Length - underScore - 1);
        if (!_languageManager.TryGetLanguage(languageName.ToString(), out var language))
            throw new ArgumentException($"Unable to get language form file name '{path.ToString()}'.", nameof(path));
        
        return language;
    }

    public void WriteLocalizationFile(FileSystemStream fileStream, LocalizationFile localizationFile)
    {
        var writer = new LocalizationFileWriter(warningAsError, serviceProvider);
        writer.WriteFile(fileStream, localizationFile);
    }

    public void WriteDiffFile(FileSystemStream fileStream, MasterTextDifference diffEntries, LanguageType language)
    {
        using var writer = new StreamWriter(fileStream, Encoding.Unicode, 1024, true);
        var locFileWriter = new LocalizationFileWriter(warningAsError, serviceProvider);
        locFileWriter.CreateDiffFile(writer, diffEntries, language);
    }

    private void LogOrThrow(string message)
    {
        if (warningAsError)
            throw new InvalidOperationException(message);
        _logger?.LogWarning(message);
    }
}