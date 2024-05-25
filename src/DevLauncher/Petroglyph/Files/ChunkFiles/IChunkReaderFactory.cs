using System.IO;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files.ChunkFiles;

public interface IChunkReaderFactory
{
    IChunkFileReader GetReaderFromStream(Stream dataStream);
}