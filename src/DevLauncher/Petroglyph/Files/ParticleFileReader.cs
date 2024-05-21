using System;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Files;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files;

internal class ParticleFileReader(string fileName, ChunkReader reader, ChunkMetadata firstChunk) : ChunkFileReaderBase<AlamoParticle>(fileName, reader, firstChunk)
{
    public override AlamoParticle ReadFile()
    {
        return new AlamoParticle();
    }
}