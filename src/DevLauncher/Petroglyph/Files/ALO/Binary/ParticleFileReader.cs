using RepublicAtWar.DevLauncher.Petroglyph.Files.ALO.Data;
using RepublicAtWar.DevLauncher.Petroglyph.Files.ChunkFiles;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files.ALO.Binary;

internal class ParticleFileReader(string fileName, ChunkReader reader, ChunkMetadata firstChunk) : ChunkFileReaderBase<AlamoParticle>(fileName, reader, firstChunk)
{
    public override AlamoParticle ReadFile()
    {
        return new AlamoParticle();
    }
}