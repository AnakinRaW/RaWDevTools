using RepublicAtWar.DevLauncher.Petroglyph.Files.ALO.Services;
using System.Collections.Generic;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files.ALO.Data;

public class AlamoModel : IAloDataContent
{
    public IList<string> Bones { get; init; }

    public ISet<string> Shaders { get; init; }

    public ISet<string> Textures { get; init; }

    public ISet<string> Proxies { get; init; }

    public void Dispose()
    {
    }
}

public class AlamoParticle : IAloDataContent
{
    public void Dispose()
    {
    }
}
