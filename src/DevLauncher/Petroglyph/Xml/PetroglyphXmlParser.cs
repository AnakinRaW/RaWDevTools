using System;
using System.IO;
using System.Xml.Linq;

namespace RepublicAtWar.DevLauncher.Petroglyph.Xml;

public abstract class PetroglyphXmlParser<T>(IServiceProvider serviceProvider) : PetroglyphXmlElementParser<T>(serviceProvider), IPetroglyphXmlParser<T>
{
    public T ParseFile(Stream xmlStream)
    {
        var doc = XDocument.Load(xmlStream);
        var root = doc.Root;
        if (root is null)
            return default;
        return Parse(root);
    }

    object? IPetroglyphXmlParser.ParseFile(Stream stream)
    {
        return ParseFile(stream);
    }
}