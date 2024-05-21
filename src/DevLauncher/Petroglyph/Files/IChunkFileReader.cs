using System;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Files;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files;

public interface IChunkFileReader : IDisposable
{
    IChunkFile ReadFile();
}

public interface IChunkFileReader<out T> : IChunkFileReader where T : IChunkFile
{
    new T ReadFile();
}