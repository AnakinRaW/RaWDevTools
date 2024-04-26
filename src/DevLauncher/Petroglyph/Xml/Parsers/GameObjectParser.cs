using System;
using System.Xml.Linq;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph.Xml.Parsers;

public sealed class GameObjectParser(IServiceProvider serviceProvider) : PetroglyphXmlElementParser<GameObject>(serviceProvider)
{
    public override GameObject Parse(XElement element)
    {
        return new GameObject();
    }
}