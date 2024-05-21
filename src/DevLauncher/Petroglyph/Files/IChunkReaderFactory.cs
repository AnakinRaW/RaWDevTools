using System.IO;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files;

public interface IChunkReaderFactory
{
    IChunkFileReader GetReaderFromStream(Stream dataStream);
}