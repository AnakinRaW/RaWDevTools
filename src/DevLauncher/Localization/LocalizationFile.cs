using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using AnakinRaW.CommonUtilities;
using Antlr4.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Files.DAT.Files;
using PG.StarWarsGame.Files.DAT.Services;
using PG.StarWarsGame.Infrastructure.Mods;
using static LocalizationGrammarParser;

namespace RepublicAtWar.DevLauncher.Localization;

internal class LocalizationFile
{
    public string Language { get; }

    public ICollection<KeyValuePair<string, string>> Entries { get; }

    public LocalizationFile(string language, ICollection<KeyValuePair<string, string>> entries)
    {
        ThrowHelper.ThrowIfNullOrEmpty(language);
        Language = language;
        Entries = entries;
    }
}

internal interface ILocalizationFileReader
{
    LocalizationFile ReadFile(string filePath);
}

internal class LocalizationFileReaderReader(IServiceProvider serviceProvider) : LocalizationGrammarBaseVisitor<LocalizationFile>, ILocalizationFileReader
{
    private readonly LocalizationFileValidator _validator = new(serviceProvider);

    public override LocalizationFile VisitLocalizationFile(LocalizationFileContext context)
    {
        var languageSpec = context.languageSpec();

        var langName = languageSpec.language().GetText().Trim('\'');
        _validator.ValidateLanguage(langName);

        var entryList = new List<KeyValuePair<string, string>>();
        var keys = new HashSet<string>();

        foreach (var entry in context.entryList().entry())
        {
            var key = entry.key().GetText();
            _validator.ValidateKey(key);

            if (!keys.Add(key))
                throw new InvalidLocalizationFileException($"The key '{key}' already exists.");

            var value = entry.value().IDENTIFIER()?.GetText();
            if (value is null)
            {
                value = entry.value().DQSTRING().GetText();
                value = _validator.NormalizeValue(value);
            }
            
            _validator.ValidateValue(value);

            entryList.Add(new KeyValuePair<string, string>(key, value));
        }

        return new LocalizationFile(langName, entryList);
    }

    public LocalizationFile ReadFile(string filePath)
    {
        var fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        using var fileStream = fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read);
        return FromStream(fileStream);
    }

    public LocalizationFile FromText(string text)
    {
        return FromStream(new AntlrInputStream(text));
    }

    internal LocalizationFile FromStream(Stream input)
    {
        return FromStream(new AntlrInputStream(input));
    }

    internal LocalizationFile FromStream(AntlrInputStream input)
    {
        var lexer = new LocalizationGrammarLexer(input);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new ThrowExceptionErrorListener());

        var parser = new LocalizationGrammarParser(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();
        parser.ErrorHandler = new StrictErrorStrategy();

        return VisitLocalizationFile(parser.localizationFile());
    }

    private class ThrowExceptionErrorListener : BaseErrorListener, IAntlrErrorListener<int>
    {
        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine,
            string msg, RecognitionException e)
        {
            throw new SyntaxErrorException($"Syntax error: {msg} (line:{line}, position:{charPositionInLine})", e);
        }

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
            string msg, RecognitionException e)
        {
            throw new SyntaxErrorException($"Syntax error: {msg} (line:{line}, position:{charPositionInLine})", e);
        }
    }

    private class StrictErrorStrategy : DefaultErrorStrategy
    {
        public override void Recover(Parser recognizer, RecognitionException e)
        {
            var token = recognizer.CurrentToken;
            var message = $"Parse error at line {token.Line}, position {token.Column} right before {GetTokenErrorDisplay(token)} ";
            throw new SyntaxErrorException(message, e);
        }


        public override IToken RecoverInline(Parser recognizer)
        {
            var token = recognizer.CurrentToken;
            var message = $"Parse error at line {token.Line}, position {token.Column} right before {GetTokenErrorDisplay(token)} ";
            throw new SyntaxErrorException(message, new InputMismatchException(recognizer));
        }

        public override void Sync(Parser recognizer)
        {
        }
    }
}

internal class LocalizationFileValidator
{
    private readonly ILogger? _logger;

    public LocalizationFileValidator(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public void ValidateLanguage(string language)
    {

    }

    public void ValidateKey(string key)
    {
    }

    public void ValidateValue(string value)
    {
    }

    public string NormalizeValue(string value)
    {
        var span = value.AsSpan();
        var withoutEnclosingQuotes = span.Slice(1, span.Length - 2);

        var newValue = withoutEnclosingQuotes.ToString();
        newValue = newValue.Replace("\"\"", "\"");
        newValue = newValue.Replace("\\\"", "\"");

        newValue = newValue.Replace("\r\n", string.Empty);
        newValue = newValue.Replace("\n", string.Empty);

        return newValue;
    }
}

internal class InvalidLocalizationFileException : Exception
{
    public InvalidLocalizationFileException(string message)
    {
    }
}


internal class LocalizationFileWriter(IPhysicalMod mod, IServiceProvider serviceProvider)
{
    private readonly IPhysicalMod _mod = mod ?? throw new ArgumentNullException(nameof(mod));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public void DatToLocalizationFile(string datFilePath)
    {
        var fs = _serviceProvider.GetRequiredService<IFileSystem>();
        var datService = _serviceProvider.GetRequiredService<IDatFileService>();

        var modPath = fs.Path.GetFullPath(_mod.Directory.FullName);

        var masterTextFile = datService.LoadAs(fs.Path.Combine(modPath, datFilePath),
            DatFileType.OrderedByCrc32);

        var modelService = _serviceProvider.GetRequiredService<IDatModelService>();
        if (modelService.GetDuplicateEntries(masterTextFile.Content).Any())
            throw new InvalidOperationException();

        var localizationFilePath = fs.Path.ChangeExtension(masterTextFile.FilePath, "txt");

        using var locFs = fs.FileStream.New(localizationFilePath, FileMode.Create);
        using var tw = new StreamWriter(locFs);

        var languageName = fs.Path.GetFileNameWithoutExtension(datFilePath).Split('_').Last().ToUpperInvariant();

        tw.WriteLine($"LANGUAGE='{languageName}';");

        foreach (var entry in masterTextFile.Content.OrderBy(e => e.Key))
        {
            var value = entry.Value;

            if (value.Contains("\""))
                value = value.Replace("\"", "\"\"");

            tw.WriteLine($"{entry.Key}=\"{value}\"");
        }
    }
}
