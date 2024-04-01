using System.Collections.Generic;
using System.Data;
using System.IO;
using AnakinRaW.CommonUtilities;
using Antlr4.Runtime;
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

internal static class LocalizationFileReader
{
    internal static LocalizationFile ReadFromText(string text)
    {
        var s = new AntlrInputStream(text);
        var l = new LocalizationGrammarLexer(s);
        l.RemoveErrorListeners();
        l.AddErrorListener(new ThrowExceptionErrorListener());
        var ts = new CommonTokenStream(l);
        var p = new LocalizationGrammarParser(ts);
        p.RemoveErrorListeners();
        p.ErrorHandler = new StrictErrorStrategy();
        var v = new LocalizationFileVisitor();
        return v.VisitLocalizationFile(p.localizationFile());
    }
}

class ThrowExceptionErrorListener : BaseErrorListener, IAntlrErrorListener<int>
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

public class StrictErrorStrategy : DefaultErrorStrategy
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


    public override void Sync(Parser recognizer) { }
}

internal class LocalizationFileVisitor : LocalizationGrammarBaseVisitor<LocalizationFile>
{
    public override LocalizationFile VisitLocalizationFile(LocalizationFileContext context)
    {
        var language = context.languageSpec();

        var langName = language.LANGUAGE_NAME().GetText();

        return new LocalizationFile(langName, new List<KeyValuePair<string, string>>());
    }
}