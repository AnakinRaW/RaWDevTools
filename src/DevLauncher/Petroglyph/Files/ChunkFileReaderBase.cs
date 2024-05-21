using System;
using AnakinRaW.CommonUtilities;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Files;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files;

internal abstract class ChunkFileReaderBase<T>(string fileName, ChunkReader chunkReader, ChunkMetadata firstChunk)
    : DisposableObject, IChunkFileReader<T>
    where T : IChunkFile
{
    protected readonly ChunkReader ChunkReader = chunkReader ?? throw new ArgumentNullException(nameof(chunkReader));

    protected string FileName { get; } = fileName;

    protected ChunkMetadata FirstChunk { get; } = firstChunk;

    public abstract T ReadFile();

    IChunkFile IChunkFileReader.ReadFile()
    {
        return ReadFile();
    }

    protected override void DisposeManagedResources()
    {
        base.DisposeManagedResources();
        ChunkReader.Dispose();
    }
}