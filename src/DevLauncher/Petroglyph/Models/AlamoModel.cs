using System.Collections.Generic;

namespace RepublicAtWar.DevLauncher.Petroglyph.Models;

public class AlamoModel
{
    public string FileName { get; }

    public IList<string> Bones { get; }

    public ISet<string> Shaders { get; }

    public ISet<string> Textures { get; }
}