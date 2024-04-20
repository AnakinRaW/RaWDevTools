using System.Collections.Generic;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files;

public struct Chunk
{
    public uint Type { get; }

    public uint Size { get; }

    public byte[]? Data { get; }

    public bool IsMiniChunk { get; }

    public bool IsChunkContainer { get; }

    public IList<Chunk> Chunks { get; }
}