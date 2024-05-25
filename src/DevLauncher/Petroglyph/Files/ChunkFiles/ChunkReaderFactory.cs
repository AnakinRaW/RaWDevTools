using System;
using System.IO;
using System.IO.Abstractions;
using PG.StarWarsGame.Files.MEG.Utilities;
using RepublicAtWar.DevLauncher.Petroglyph.Files.ALO.Binary;
using RepublicAtWar.DevLauncher.Petroglyph.Files.ALO.Services;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files.ChunkFiles;

internal class ChunkReaderFactory : IChunkReaderFactory
{
    public IChunkFileReader GetReaderFromStream(Stream dataStream)
    {
        var fileName = GetFileName(dataStream);

        var chuckReader = new ChunkReader(dataStream);

        var firstChunk = chuckReader.ReadChunk();

        if (firstChunk.Type == (int)ChunkType.Skeleton)
            return new ModelFileReader(fileName, chuckReader, firstChunk);

        if (firstChunk.Type == (int)ChunkType.Particle)
            return new ParticleFileReader(fileName, chuckReader, firstChunk);

        throw new NotImplementedException();
    }

    private string GetFileName(Stream stream)
    {
        if (stream is FileSystemStream fs)
            return fs.Name;
        if (stream is MegFileDataStream megStream)
            return megStream.EntryPath;
        return "UNKNOWN FILE";
    }
}

public enum ChunkType
{
    Unknown,
    Skeleton = 0x200,
    BoneCount = 0x201,
    Bone = 0x202,
    BoneName = 0x203,
    Mesh = 0x400,
    MeshName = 0x401,
    MeshInformation = 0x402,
    Light = 0x1300,
    Connections = 0x600,
    ProxyConnection = 0x603,
    Particle = 0x900,
    Animation = 0x1000,
    SubMeshData = 0x00010000,
    SubMeshMaterialInformation = 0x00010100,
    ShaderFileName = 0x00010101,
    ShaderTexture = 0x00010105,
}

public readonly struct ChunkMetadata
{
    public readonly int Type;
    public readonly int Size;

    private ChunkMetadata(int type, int size, bool isContainer, bool isMiniChunk)
    {
        Type = type;
        Size = size;
        IsMiniChunk = isMiniChunk;
        IsContainer = isContainer;
    }

    public bool IsContainer { get; }

    public bool IsMiniChunk { get; }

    public static ChunkMetadata FromContainer(int type, int size)
    {
        return new ChunkMetadata(type, size, true, false);
    }

    public static ChunkMetadata FromData(int type, int size, bool isMini = false)
    {
        return new ChunkMetadata(type, size, false, isMini);
    }
}