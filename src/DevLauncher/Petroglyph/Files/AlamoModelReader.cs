using System;
using System.IO;
using System.Text;
using RepublicAtWar.DevLauncher.Petroglyph.Models;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files;

internal class AlamoModelReader
{
    public AlamoModel ReadModel(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        using var chunkReader = new ChunkReader(stream, Encoding.ASCII);

        return default;
    }
}