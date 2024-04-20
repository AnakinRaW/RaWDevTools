using System.IO;

namespace RepublicAtWar.DevLauncher.Petroglyph.Xml;

public interface IPetroglyphXmlParser : IPetroglyphXmlElementParser
{
    public object? ParseFile(Stream stream);
}

public interface IPetroglyphXmlParser<out T> : IPetroglyphXmlElementParser<T>, IPetroglyphXmlParser
{
    public new T ParseFile(Stream stream);
}