using System;
using System.IO;
using System.Text;
using AnakinRaW.CommonUtilities;
using PG.Commons.Utilities;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files.ChunkFiles;

internal class ChunkReader : DisposableObject
{
    private readonly BinaryReader _binaryReader;

    public ChunkReader(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        _binaryReader = new BinaryReader(stream);
    }

    protected override void DisposeManagedResources()
    {
        base.DisposeManagedResources();
        _binaryReader.Dispose();
    }

    public ChunkMetadata ReadChunk()
    {
        var type = _binaryReader.ReadInt32();
        var rawSize = _binaryReader.ReadInt32();

        var isContainer = (rawSize & 0x80000000) != 0;
        var size = rawSize & 0x7FFFFFFF;

        return isContainer ? ChunkMetadata.FromContainer(type, size) : ChunkMetadata.FromData(type, size);
    }

    public ChunkMetadata ReadChunk(ref int readBytes)
    {
        var chunk = ReadChunk();
        readBytes += 8;
        return chunk;
    }

    public ChunkMetadata ReadMiniChunk(ref int readBytes)
    {
        var type = _binaryReader.ReadByte();
        var size = _binaryReader.ReadByte();

        readBytes += 2;

        return ChunkMetadata.FromData(type, size, true);
    }

    public uint ReadDword(ref int readSize)
    {
        var value = _binaryReader.ReadUInt32();
        readSize += sizeof(uint);
        return value;
    }

    public void Skip(int bytesToSkip, ref int readBytes)
    {
        _binaryReader.BaseStream.Seek(bytesToSkip, SeekOrigin.Current);
        readBytes += bytesToSkip;
    }

    public void Skip(int bytesToSkip)
    {
        _binaryReader.BaseStream.Seek(bytesToSkip, SeekOrigin.Current);
    }

    public string ReadString(int size, Encoding encoding, bool zeroTerminated, ref int readSize)
    {
        var value = _binaryReader.ReadString(size, encoding, zeroTerminated);
        readSize += size;
        return value;
    }

    public ChunkMetadata? TryReadChunk()
    {
        if (_binaryReader.BaseStream.Position == _binaryReader.BaseStream.Length)
            return null;
        try
        {
            return ReadChunk();
        }
        catch (EndOfStreamException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}