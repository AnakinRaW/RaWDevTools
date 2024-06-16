using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Engine.Language;
using PG.StarWarsGame.Files.DAT.Data;
using PG.StarWarsGame.Files.DAT.Files;
using PG.StarWarsGame.Files.DAT.Services;

namespace RepublicAtWar.DevTools.Localization;

internal class LocalizationFileWriter(bool warningAsError, IServiceProvider serviceProvider)
{
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(LocalizationFileWriter));
    private readonly IDatFileService _datService = serviceProvider.GetRequiredService<IDatFileService>();
    private readonly IDatModelService _modelService = serviceProvider.GetRequiredService<IDatModelService>();
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    private bool WarningAsError { get; } = warningAsError;

    public void WriteFile(FileSystemStream fileStream, LocalizationFile localizationFile)
    {
        using var writer = new StreamWriter(fileStream, Encoding.Unicode, 1024, true);

        WriteLanguage(localizationFile.Language, writer);

        WriteInstructions(writer);

        foreach (var entry in localizationFile.Entries) 
            WriteEntry(entry, writer);
    }

    public IDatModel DatToLocalizationFile(string datFilePath, LanguageType language)
    {
        var masterText = LoadAndRemoveDuplicates(datFilePath);

        var localizationFilePath = _fileSystem.Path.ChangeExtension(datFilePath, "txt");

        using var locFs = _fileSystem.FileStream.New(localizationFilePath, FileMode.Create);
        using var writer = new StreamWriter(locFs);

        WriteLanguage(language, writer);

        WriteInstructions(writer);

        foreach (var entry in masterText.OrderBy(e => e.Key))
            WriteEntry(entry, writer);

        return masterText;
    }


    public void InitializeFromDatAndEnglishReference(string datFilePath, IDatModel referenceModel, LanguageType language)
    {
        var masterText = LoadAndRemoveDuplicates(datFilePath);

        var localizationFilePath = _fileSystem.Path.ChangeExtension(datFilePath, "txt");

        using var locFs = _fileSystem.FileStream.New(localizationFilePath, FileMode.Create);
        using var writer = new StreamWriter(locFs);

        var normalEntries = new List<DatStringEntry>();
        var entriesWithMissingValue = new List<DatStringEntry>();
        var englishValueEntries = new List<DatStringEntry>();

        foreach (var entry in masterText)
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

        WriteLanguage(language, writer);

        WriteInstructions(writer);
        
        foreach (var entry in normalEntries.OrderBy(e => e.Key)) 
            WriteEntry(entry, writer);

        writer.WriteLine();
        writer.WriteLine();

        if (englishValueEntries.Count > 0)
        {
            foreach (var entry in englishValueEntries.Union(entriesWithMissingValue).OrderBy(e => e.Key))
                WriteEntry(entry, writer);
        }

    }

    public void CreateDiffFile(FileSystemStream fileStream, MasterTextDifference diffEntries, LanguageType language)
    {
        using var writer = new StreamWriter(fileStream, Encoding.Unicode, 1024, true);

        WriteLanguage(language, writer);
        WriteInstructions(writer);

        if (diffEntries.DeletedKeys.Count > 0)
            WriteCommentSection("The following entries have been deleted - DO NOT TRANSLATE!", writer);
        foreach (var deletedKey in diffEntries.DeletedKeys) 
            WriteEntry(deletedKey, LocalizationEntry.DeletedKeyValue, writer);

        if (diffEntries.NewEntries.Count > 0)
            WriteCommentSection("The following entries are new", writer);
        foreach (var newEntry in diffEntries.NewEntries) WriteEntry(newEntry, writer);

        if (diffEntries.ChangedEntries.Count > 0)
            WriteCommentSection("The following entries have been changed in the english language", writer);
        foreach (var (entry, oldValue) in diffEntries.ChangedEntries)
        {
            WriteComment($"ORG Value: \"{EscapeQuotes(oldValue)}\"", writer);
            WriteEntry(entry, writer);
            writer.WriteLine();
        }
    }

    private void WriteCommentSection(string comment, StreamWriter writer)
    {
        writer.WriteLine();
        WriteComment(comment, writer);
        writer.WriteLine();
    }

    private void WriteComment(string comment, TextWriter writer)
    {
        writer.WriteLine($"# {comment}");
    }


    private void WriteLanguage(LanguageType language, TextWriter writer)
    {
        writer.WriteLine($"LANGUAGE='{language.ToString().ToUpperInvariant()}';");
    }

    private void WriteInstructions(StreamWriter tw)
    {
        tw.WriteLine();
        WriteCommentLine(" Instructions how to use this data:", tw);
        WriteCommentLine(" This is a simple key-value data holding localized game strings. Entries are not required to be sorted in this data.", tw);
        WriteCommentLine(" The format of an entry is always: KEY=\"Value\"", tw);
        WriteCommentLine(" The key shall be UPPERCASE only. Other allowed characters are numbers and '_' and '-'", tw);
        WriteCommentLine(" The value shall be enclosed in double quotes '\"'. This way a value can have line breaks.", tw);
        WriteCommentLine(" If you wish to use a double quote inside the value either use '\"\"' [2 times double quote] or \\\" [backslash + double quote]", tw);
        tw.WriteLine();
        tw.WriteLine();
    }

    private IDatModel LoadAndRemoveDuplicates(string datFilePath)
    {
        var masterText = _datService.LoadAs(datFilePath, DatFileType.OrderedByCrc32).Content;

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
    
    private void WriteCommentLine(string comment, TextWriter writer)
    {
        writer.WriteLine("#" + comment);
    }


    private void WriteEntry(DatStringEntry entry, TextWriter writer)
    {
        WriteEntry(entry.Key, entry.Value, writer);
    }

    private void WriteEntry(LocalizationEntry entry, TextWriter writer)
    {
        WriteEntry(entry.Key, entry.Value, writer);
    }

    private void WriteEntry(string key, string value, TextWriter writer)
    {
        if (value.Contains("\""))
            value = EscapeQuotes(value);

        if (value.IndexOfAny(['\r', '\n', '\t'], 0) != -1)
            LogOrThrow($"Entry of key '{key}' has invalid escape sequence.");

        writer.WriteLine($"{key}=\"{value}\"");
    }

    private string EscapeQuotes(string value)
    {
        return value.Replace("\"", "\"\"");
    }

    private void LogOrThrow(string message)
    {
        if (WarningAsError)
            throw new InvalidOperationException(message);
        _logger?.LogWarning(message);
    }
}