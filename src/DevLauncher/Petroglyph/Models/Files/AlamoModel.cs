using System.Collections.Generic;

namespace RepublicAtWar.DevLauncher.Petroglyph.Models.Files;

public interface IChunkFile
{
    public string FileName { get; }
}

public class AlamoModel : IChunkFile
{
    public IList<string> Bones { get; init; }

    public ISet<string> Shaders { get; init; }

    public ISet<string> Textures { get; init; }

    public ISet<string> Proxies { get; init; }

    public string FileName { get; init; }

    public bool HasCollision { get; init; }
}

public class AlamoParticle : IChunkFile
{
    public string FileName { get; }
}

public class AlamoMap : IChunkFile
{
    public string FileName { get; }
}