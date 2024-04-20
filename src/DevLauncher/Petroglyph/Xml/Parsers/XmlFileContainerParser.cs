using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph.Xml.Parsers;

public class XmlFileContainerParser : PetroglyphXmlParser<XmlFileContainer>
{
    protected override IDictionary<string, Type> Map { get; } = new Dictionary<string, Type>
    {
        { "File", typeof(string) }
    };

    public override XmlFileContainer Parse(XElement element)
    {
        var xmlValues = ToKeyValuePairList(element);

        return xmlValues.TryGetValues("File", out var files)
            ? new XmlFileContainer(files.OfType<string>().ToList())
            : new XmlFileContainer([]);
    }
}