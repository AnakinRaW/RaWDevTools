using System;
using System.Collections.Generic;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;
using RepublicAtWar.DevLauncher.Petroglyph.Xml.Parsers;

namespace RepublicAtWar.DevLauncher.Petroglyph.Xml;

public sealed class PetroglyphXmlParserFactory
{
    public static readonly PetroglyphXmlParserFactory Instance = new();

    private PetroglyphXmlParserFactory()
    {
    }

    public IPetroglyphXmlElementParser<T> GetParser<T>(IServiceProvider serviceProvider)
    {
        return (IPetroglyphXmlElementParser<T>)GetParser(typeof(T), serviceProvider);
    }

    public IPetroglyphXmlElementParser GetParser(Type type, IServiceProvider serviceProvider)
    {
        if (type == typeof(GameObject))
            return new GameObjectParser(serviceProvider);
        if (type == typeof(string))
            return PetroglyphXmlStringParser.Instance;
        throw new NotImplementedException();
    }

    public IPetroglyphXmlParser<T> GetFileParser<T>(IServiceProvider serviceProvider)
    {
        return (IPetroglyphXmlParser<T>)GetFileParser(typeof(T), serviceProvider);
    }

    public IPetroglyphXmlParser GetFileParser(Type type, IServiceProvider serviceProvider)
    {
        if (type == typeof(XmlFileContainer))
            return new XmlFileContainerParser(serviceProvider);
        if (type == typeof(GameConstants))
            return new GameConstantsParser(serviceProvider);
        if (type == typeof(IList<GameObject>))
            return new GameObjectFileParser(serviceProvider);

        throw new NotImplementedException($"The parser for the type {type} is not yet implemented.");
    }
}