using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace RepublicAtWar.DevLauncher.Petroglyph.Xml;

public abstract class PetroglyphXmlElementParser<T> : IPetroglyphXmlElementParser<T>
{
    protected virtual IDictionary<string, Type> Map { get; } = new Dictionary<string, Type>();

    private readonly PetroglyphXmlParserFactory _parserFactory = PetroglyphXmlParserFactory.Instance;

    public abstract T Parse(XElement element);

    public ValueListDictionary<string, object> ToKeyValuePairList(XElement element)
    {
        var keyValuePairList = new ValueListDictionary<string, object>();
        foreach (var elm in element.Elements())
        {
            var tagName = elm.Name.LocalName;

            if (!Map.ContainsKey(tagName))
                continue;

            var parser = _parserFactory.GetParser(Map[tagName]);
            var value = parser.Parse(elm);

            keyValuePairList.Add(tagName, value);
        }

        return keyValuePairList;
    }

    object? IPetroglyphXmlElementParser.Parse(XElement element)
    {
        return Parse(element);
    }
}