using System;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;
using RepublicAtWar.DevLauncher.Petroglyph.Xml.Parsers;

namespace RepublicAtWar.DevLauncher.Petroglyph.Xml;

public sealed class PetroglyphXmlParserFactory
{
    public static readonly PetroglyphXmlParserFactory Instance = new();

    private PetroglyphXmlParserFactory()
    {
    }

    public IPetroglyphXmlElementParser<T> GetParser<T>()
    {
        return (IPetroglyphXmlElementParser<T>)GetParser(typeof(T));
    }

    public IPetroglyphXmlElementParser GetParser(Type type)
    {
        if (type == typeof(string))
            return PetroglyphXmlStringParser.Instance;
        throw new NotImplementedException();
    }

    public IPetroglyphXmlParser<T> GetFileParser<T>()
    {
        return (IPetroglyphXmlParser<T>)GetFileParser(typeof(T));
    }

    public IPetroglyphXmlParser GetFileParser(Type type)
    {
        if (type == typeof(XmlFileContainer))
            return new XmlFileContainerParser();
        if (type == typeof(GameConstants))
            return new GameConstantsParser();

        throw new NotImplementedException();
    }


}