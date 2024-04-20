using System.Collections.Generic;

namespace RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;

public class XmlFileContainer(IList<string> files)
{
    public IList<string> Files { get; } = files;
}