using System.Xml.Linq;

namespace RepublicAtWar.DevLauncher.Petroglyph.Xml.Parsers;

public sealed class PetroglyphXmlStringParser : PetroglyphXmlElementParser<string>
{
    public static readonly PetroglyphXmlStringParser Instance = new();

    private PetroglyphXmlStringParser()
    {
    }

    public override string Parse(XElement element)
    {
        return element.Value.Trim();
    }
}