using System;
using System.IO;
using System.IO.Abstractions;
using System.Xml;
using System.Xml.Linq;
using PG.StarWarsGame.Files.MEG.Utilities;

namespace RepublicAtWar.DevLauncher.Petroglyph.Xml;

public abstract class PetroglyphXmlParser<T>(IServiceProvider serviceProvider) : PetroglyphXmlElementParser<T>(serviceProvider), IPetroglyphXmlParser<T>
{
    protected virtual bool LoadLineInfo => false;
    public T ParseFile(Stream xmlStream)
    {
        var fileName = GetFileNameFromStream(xmlStream);
        var xmlReader = XmlReader.Create(xmlStream, new XmlReaderSettings(), fileName);

        var options = LoadOptions.SetBaseUri;
        if (LoadLineInfo)
            options |= LoadOptions.SetLineInfo;

        var doc = XDocument.Load(xmlReader, options);
        var root = doc.Root;
        if (root is null)
            return default;
        return Parse(root);
    }

    object? IPetroglyphXmlParser.ParseFile(Stream stream)
    {
        return ParseFile(stream);
    }

    private static string GetFileNameFromStream(Stream stream)
    {
        if (stream is FileSystemStream fs)
            return fs.Name;
        if (stream is MegFileDataStream megStream)
            return megStream.EntryPath;
        return string.Empty;
    }
}

public readonly struct XmlLocationInfo(string xmlFile, int? line)
{
    public bool HasLocation => !string.IsNullOrEmpty(XmlFile) && Line is not null;

    public string? XmlFile { get; } = xmlFile;

    public int? Line { get; } = line;


    public static XmlLocationInfo FromElement(XElement element)
    {
        if (element is IXmlLineInfo lineInfoHolder && lineInfoHolder.HasLineInfo())
            return new XmlLocationInfo(element.Document.BaseUri, lineInfoHolder.LineNumber);
        return new XmlLocationInfo(element.Document.BaseUri, null);
    }


    public override string ToString()
    {
        if (string.IsNullOrEmpty(XmlFile))
            return "No File information";
        return Line is null ? XmlFile! : $"{XmlFile} at line: {Line}";
    }
}