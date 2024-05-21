using System;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Files;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files;

internal class MapFileReader(string fileName, ChunkReader reader, ChunkMetadata firstChunk) : ChunkFileReaderBase<AlamoMap>(fileName, reader, firstChunk)
{
    public override AlamoMap ReadFile()
    {
        throw new NotImplementedException();
    }
}