using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Abstractions;
using Antlr4.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RepublicAtWar.DevLauncher.Localization;

internal class LocalizationFileReader(bool warningAsError, IServiceProvider serviceProvider) : LocalizationGrammarBaseVisitor<LocalizationFile>, ILocalizationFileReader
{
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()
        ?.CreateLogger(typeof(LocalizationFileReader));

    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    private readonly LocalizationFileValidator _validator = new(warningAsError, serviceProvider);

    public override LocalizationFile VisitLocalizationFile(LocalizationGrammarParser.LocalizationFileContext context)
    {
        var languageSpec = context.languageSpec();

        var langName = languageSpec.language().GetText().Trim('\'');
        _validator.ValidateLanguage(langName);

        var listContext = context.entryList();
        if (listContext is null)
            return new LocalizationFile(langName, Array.Empty<LocalizationEntry>());
        
        var entryList = new List<LocalizationEntry>();
        var keys = new HashSet<string>();

        var duplicates = new HashSet<string>();

        foreach (var entry in listContext.entry())
        {
            var key = entry.key().GetText();
            _validator.ValidateKey(key);

            if (!keys.Add(key)) 
                duplicates.Add(key);

            var value = GetTextFromValueContext(entry.value(), key);
            if (value is null)
                throw new InvalidLocalizationFileException($"The key '{key}' produced a null value. Grammar error?");
            
            _validator.ValidateValue(key, value);

            entryList.Add(new LocalizationEntry(key, value));
        }

        if (duplicates.Count > 0)
            throw new DuplicateKeysException(duplicates);

        return new LocalizationFile(langName, entryList);
    }

    private string? GetTextFromValueContext(LocalizationGrammarParser.ValueContext valueContext, string key)
    {
        var identifier = valueContext.IDENTIFIER();
        if (identifier is not null)
        {
            LogOrThrow($"The value of the key '{key}' is not enclosed by double quotes.");
            return identifier.GetText();
        }

        var value = valueContext.VALUE();
        if (value is not null)
        {
            LogOrThrow($"The value of the key '{key}' is not enclosed by double quotes.");
            return ReplaceEscapeSequences(value.GetText());
        }

        var dqString = valueContext.DQSTRING();
        if (dqString is not null)
        {
            var dqValue = dqString.GetText();
            dqValue = dqValue.Substring(1, dqValue.Length - 2);

            dqValue = ReplaceEscapeSequences(dqValue);

            dqValue = dqValue.Replace("\"\"", "\"");

            dqValue = dqValue.Replace("\r\n", string.Empty);
            dqValue = dqValue.Replace("\n", string.Empty);

            return dqValue;
        }
        return null;
    }

    private static string ReplaceEscapeSequences(string value)
    {
        value = value.Replace("\\'", "'");
        value = value.Replace("\\\"", "\"");
        value = value.Replace("\\=", "=");
        value = value.Replace("\\#", "#");

        return value;
    }

    public LocalizationFile ReadFile(string filePath)
    {
        using var fileStream = _fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read);
        var localizationFile = FromStream(fileStream);

        var langName = LanguageNameFromFileName(filePath);
        if (localizationFile.Language != langName)
            LogOrThrow($"The file name of '{filePath}' does not match the language content '{langName}'.");

        return localizationFile;
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

    private string? LanguageNameFromFileName(string fileName)
    {
        if (fileName == null) 
            throw new ArgumentNullException(nameof(fileName));
        var fileNameWithoutExtension = _fileSystem.Path.GetFileNameWithoutExtension(fileName);
        var underScore = fileNameWithoutExtension.LastIndexOf('_');
        if (underScore == -1)
            return null!;
        return fileNameWithoutExtension.Substring(underScore + 1, fileNameWithoutExtension.Length - underScore - 1)
            .ToUpperInvariant();
    }

    private void LogOrThrow(string message)
    {
        if (warningAsError)
            throw new InvalidLocalizationFileException(message);
        _logger?.LogWarning(message);
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