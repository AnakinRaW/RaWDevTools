using System;
using System.Collections.Generic;
using System.Xml.Linq;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph.Xml.Parsers;

public class GameObjectFileParser(IServiceProvider serviceProvider) : PetroglyphXmlParser<IList<GameObject>>(serviceProvider)
{
    public override IList<GameObject> Parse(XElement element)
    {
        var elements = new List<GameObject>();
        var parser = PetroglyphXmlParserFactory.Instance.GetParser<GameObject>(ServiceProvider);
        foreach (var gameObjectElement in element.Elements()) 
            elements.Add(parser.Parse(gameObjectElement));
        return elements;
    }
}