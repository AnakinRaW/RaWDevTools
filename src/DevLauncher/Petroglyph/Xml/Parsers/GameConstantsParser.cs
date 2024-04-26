using System;
using System.Xml.Linq;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph.Xml.Parsers;

public class GameConstantsParser(IServiceProvider serviceProvider) : PetroglyphXmlParser<GameConstants>(serviceProvider)
{
    public override GameConstants Parse(XElement element)
    {
        return new GameConstants();
    }
}