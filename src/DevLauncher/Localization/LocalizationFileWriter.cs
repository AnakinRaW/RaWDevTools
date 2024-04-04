using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Files.DAT.Data;
using PG.StarWarsGame.Files.DAT.Files;
using PG.StarWarsGame.Files.DAT.Services;

namespace RepublicAtWar.DevLauncher.Localization;

internal class LocalizationFileWriter(bool warningAsError, IServiceProvider serviceProvider)
{
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(LocalizationFileWriter));
    private readonly IDatFileService _datService = serviceProvider.GetRequiredService<IDatFileService>();
    private readonly IDatModelService _modelService = serviceProvider.GetRequiredService<IDatModelService>();
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    private bool WarningAsError { get; } = warningAsError;

    public void AppendEntries(string localizationFile, ICollection<DatStringEntry> entries)
    {
        if (entries.Count == 0)
            return;

        using var writer = _fileSystem.File.AppendText(localizationFile);

        writer.WriteLine();
        writer.WriteLine();

        foreach (var entry in entries) 
            WriteEntry(entry, writer);
    }

    public IDatModel DatToLocalizationFile(string datFilePath)
    {
        var masterText = LoadAndRemoveDuplicates(datFilePath);

        var localizationFilePath = _fileSystem.Path.ChangeExtension(datFilePath, "txt");

        using var locFs = _fileSystem.FileStream.New(localizationFilePath, FileMode.Create);
        using var writer = new StreamWriter(locFs);

        writer.WriteLine($"LANGUAGE='{GetLanguageName(datFilePath)}';");

        WriteInstructions(writer);

        foreach (var entry in masterText.OrderBy(e => e.Key))
            WriteEntry(entry, writer);

        return masterText;
    }


    public void InitializeFromDatAndEnglishReference(string datFilePath, IDatModel referenceModel)
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

        writer.WriteLine($"LANGUAGE='{GetLanguageName(datFilePath)}';");

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

    private void WriteInstructions(StreamWriter tw)
    {
        tw.WriteLine();
        WriteCommentLine(" Instructions how to use this file:", tw);
        WriteCommentLine(" This is a simple key-value file holding localized game strings. Entries are not required to be sorted in this file.", tw);
        WriteCommentLine(" The format of an entry is always: KEY=\"Value\"", tw);
        WriteCommentLine(" The key shall be UPPERCASE only. Other allowed characters are numbers and '_' and '-'", tw);
        WriteCommentLine(" The value shall be enclosed in double quotes '\"'. This way a value can have line breaks.", tw);
        WriteCommentLine(" If you wish to use a double quote inside the value either use '\"\"' [2 times double quote] or \\\" [backslash + double quote]", tw);
        tw.WriteLine();
        tw.WriteLine();
    }

    private string GetLanguageName(string filePath)
    {
        return _fileSystem.Path.GetFileNameWithoutExtension(filePath).Split('_').Last().ToUpperInvariant();
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
        var value = entry.Value;

        if (value.Contains("\""))
            value = value.Replace("\"", "\"\"");

        if (value.IndexOfAny(['\r', '\n', '\t'], 0) != -1)
            LogOrThrow($"Entry of key '{entry.Key}' has invalid escape sequence.");

        writer.WriteLine($"{entry.Key}=\"{value}\"");
    }

    private void LogOrThrow(string message)
    {
        if (WarningAsError)
            throw new InvalidOperationException(message);
        _logger?.LogWarning(message);
    }
}