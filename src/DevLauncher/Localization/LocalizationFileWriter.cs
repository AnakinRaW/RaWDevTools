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

internal class LocalizationFileWriter(IServiceProvider serviceProvider)
{
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(LocalizationFileWriter));
    private readonly IDatFileService _datService = serviceProvider.GetRequiredService<IDatFileService>();
    private readonly IDatModelService _modelService = serviceProvider.GetRequiredService<IDatModelService>();
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    public IDatModel DatToLocalizationFile(string datFilePath)
    {
        var masterText = LoadAndRemoveDuplicates(datFilePath);

        var localizationFilePath = _fileSystem.Path.ChangeExtension(datFilePath, "txt");

        using var locFs = _fileSystem.FileStream.New(localizationFilePath, FileMode.Create);
        using var tw = new StreamWriter(locFs);

        tw.WriteLine($"LANGUAGE='{GetLanguageName(datFilePath)}';");

        WriteInstructions(tw);

        foreach (var entry in masterText.OrderBy(e => e.Key))
            WriteEntry(entry, tw);

        return masterText;
    }


    public void InitializeFromDatAndEnglishReference(string datFilePath, IDatModel referenceModel)
    {
        var masterText = LoadAndRemoveDuplicates(datFilePath);

        var localizationFilePath = _fileSystem.Path.ChangeExtension(datFilePath, "txt");

        using var locFs = _fileSystem.FileStream.New(localizationFilePath, FileMode.Create);
        using var tw = new StreamWriter(locFs);

        var normalEntries = new List<DatStringEntry>();

        var entriesWhichDotNotExistInReference = new List<DatStringEntry>();

        var missingEntries = new List<DatStringEntry>();

        var entriesWithMissingValue = new List<DatStringEntry>();


        foreach (var key in _modelService.GetMissingKeysFromBase(referenceModel, masterText))
        {
            missingEntries.AddRange(referenceModel.EntriesWithKey(key));
        }

        foreach (var key in _modelService.GetMissingKeysFromBase(masterText, referenceModel))
        {
            entriesWhichDotNotExistInReference.AddRange(masterText.EntriesWithKey(key));
        }

        foreach (var entry in masterText)
        {
            var englishEntry = referenceModel.EntriesWithCrc(entry.Crc32).First();

            if (entry.Value == string.Empty)
            {
                if (englishEntry.Value == string.Empty)
                {
                    normalEntries.Add(entry);
                }
                else
                {
                    entriesWithMissingValue.Add(englishEntry);
                }
            }
            else
            {
                normalEntries.Add(entry);
            }
        }

        tw.WriteLine($"LANGUAGE='{GetLanguageName(datFilePath)}';");

        WriteInstructions(tw);
        
        foreach (var entry in normalEntries.OrderBy(e => e.Key)) 
            WriteEntry(entry, tw);


        if (missingEntries.Count > 0)
        {
            WriteSectionComment("Entries which are currently missing", tw);

            foreach (var entry in missingEntries.OrderBy(e => e.Key))
                WriteEntry(entry, tw);
        }

        if (entriesWithMissingValue.Count > 0)
        {
            WriteSectionComment("Entries which are missing a text value compare to English", tw);

            foreach (var entry in entriesWithMissingValue.OrderBy(e => e.Key))
                WriteEntry(entry, tw);
        }

        if (entriesWhichDotNotExistInReference.Count > 0)
        {
            WriteSectionComment("Entries which do not exist in the English model", tw);

            foreach (var entry in entriesWhichDotNotExistInReference.OrderBy(e => e.Key))
                WriteEntry(entry, tw);
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

            _logger?.LogWarning(sb.ToString());
            masterText = _modelService.RemoveDuplicates(masterText);
        }
        return masterText;
    }

    private void WriteSectionComment(string comment, TextWriter writer)
    {
        writer.WriteLine();
        writer.WriteLine("#" + comment);
        writer.WriteLine();
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
            _logger?.LogWarning($"Entry of key '{entry.Key}' has invalid escape sequence.");

        writer.WriteLine($"{entry.Key}=\"{value}\"");
    }
}