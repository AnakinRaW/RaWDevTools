using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using Antlr4.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RepublicAtWar.DevTools.Localization;

internal class LocalizationFileReader : LocalizationGrammarBaseVisitor<LocalizationFile>, ILocalizationFileReader
{
    private readonly ILogger? _logger;

    private readonly IFileSystem _fileSystem;

    private readonly LocalizationFileValidator _validator;
    private readonly bool _warningAsError;

    private readonly Stream _dataStream = null!;

    private readonly string? _fileName;

    public LocalizationFileReader(string filePath, bool warningAsError, IServiceProvider serviceProvider) : this(warningAsError, serviceProvider)
    {
        if (filePath == null) 
            throw new ArgumentNullException(nameof(filePath));
        _dataStream = _fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read);
        _fileName = Path.GetFileName(filePath);
    }

    public LocalizationFileReader(Stream stream, bool warningAsError, IServiceProvider serviceProvider) : this(warningAsError, serviceProvider)
    {
        _dataStream = stream ?? throw new ArgumentNullException(nameof(stream));
        if (stream is FileSystemStream fsStream)
            _fileName = fsStream.Name;
        if (stream is FileStream fs)
            _fileName = fs.Name;
    }

    private LocalizationFileReader(bool warningAsError, IServiceProvider serviceProvider)
    {
        _warningAsError = warningAsError;
        _logger = serviceProvider.GetService<ILoggerFactory>()
            ?.CreateLogger(typeof(LocalizationFileReader));
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _validator = new(warningAsError, serviceProvider);
    }

    public LocalizationFile Read()
    {
        return FromStream(_dataStream);
    }

    public void Dispose()
    {
        _dataStream.Dispose();
    }

    public override LocalizationFile VisitLocalizationFile(LocalizationGrammarParser.LocalizationFileContext context)
    {
        var languageSpec = context.languageSpec();

        var langName = languageSpec.language().GetText().Trim('\'');
        var language = _validator.GetLanguage(langName);

        var listContext = context.entryList();
        if (listContext is null)
            return new LocalizationFile(language, Array.Empty<LocalizationEntry>());
        
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
            throw new DuplicateKeysException(language, duplicates);

        return new LocalizationFile(language, entryList);
    }

    internal LocalizationFile FromStream(Stream input)
    {
        return FromStream(new AntlrInputStream(input));
    }

    private LocalizationFile FromStream(AntlrInputStream input)
    {
        var lexer = new LocalizationGrammarLexer(input);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new ThrowExceptionErrorListener(_fileName));

        var parser = new LocalizationGrammarParser(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();
        parser.ErrorHandler = new StrictErrorStrategy(_fileName);

        return VisitLocalizationFile(parser.localizationFile());
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

    private void LogOrThrow(string message)
    {
        if (_warningAsError)
            throw new InvalidLocalizationFileException(message);
        _logger?.LogWarning(message);
    }

    private class ThrowExceptionErrorListener(string? fileName) : BaseErrorListener, IAntlrErrorListener<int>
    {
        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine,
            string msg, RecognitionException e)
        {
           throw new SyntaxErrorException(MessageFromPosition(line, charPositionInLine, msg), e);
        }

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
            string msg, RecognitionException e)
        {
            throw new SyntaxErrorException(MessageFromPosition(line, charPositionInLine, msg), e);
        }

        private string MessageFromPosition(int line, int charPositionInLine, string msg)
        {
            var sb = new StringBuilder("Syntax error");
            if (fileName is not null)
                sb.Append($" in file '{fileName}'");
            sb.Append($": {msg} (line:{line}, position:{charPositionInLine})");
            return sb.ToString();
        }
    }

    private class StrictErrorStrategy(string? fileName) : DefaultErrorStrategy
    {
        public override void Recover(Parser recognizer, RecognitionException e)
        {
            var message = MessageFromToken(recognizer.CurrentToken);
            throw new SyntaxErrorException(message, e);
        }

        public override IToken RecoverInline(Parser recognizer)
        {
            var message = MessageFromToken(recognizer.CurrentToken);
            throw new SyntaxErrorException(message, new InputMismatchException(recognizer));
        }

        private string MessageFromToken(IToken token)
        {
            var sb = new StringBuilder("Parse error");
            if (fileName is not null)
                sb.Append($" in file '{fileName}'");
            sb.Append($" at line {token.Line}, position {token.Column} right before {GetTokenErrorDisplay(token)}");
            return sb.ToString();
        }

        public override void Sync(Parser recognizer)
        {
        }
    }
}