using System;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files.ChunkFiles;

public interface IChunkFileReader : IDisposable
{
    IChunkData ReadFile();
}

public interface IChunkFileReader<out T> : IChunkFileReader where T : IChunkData
{
    new T ReadFile();
}

public interface IChunkData : IDisposable;