using System;
using AnakinRaW.CommonUtilities;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files.ChunkFiles;

internal abstract class ChunkFileReaderBase<T>(string fileName, ChunkReader chunkReader, ChunkMetadata firstChunk)
    : DisposableObject, IChunkFileReader<T>
    where T : IChunkData
{
    protected readonly ChunkReader ChunkReader = chunkReader ?? throw new ArgumentNullException(nameof(chunkReader));

    protected string FileName { get; } = fileName;

    protected ChunkMetadata FirstChunk { get; } = firstChunk;

    public abstract T ReadFile();

    IChunkData IChunkFileReader.ReadFile()
    {
        return ReadFile();
    }

    protected override void DisposeManagedResources()
    {
        base.DisposeManagedResources();
        ChunkReader.Dispose();
    }
}