using System;
using System.IO;
using System.Text;
using AnakinRaW.CommonUtilities;
using PG.Commons.Utilities;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files;

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

    public ChunkMetadata ReadMiniChunk()
    {
        var type = _binaryReader.ReadByte();
        var size = _binaryReader.ReadByte();

        return ChunkMetadata.FromData(type, size, true);
    }

    public uint ReadDword()
    {
        return _binaryReader.ReadUInt32();
    }

    public int Skip(int bytesToSkip)
    {
        //_binaryReader.ReadBytes(bytesToSkip);
        _binaryReader.BaseStream.Seek(bytesToSkip, SeekOrigin.Current);
        return bytesToSkip;
    }

    public int SkipNext(bool miniChunk = false)
    {
        var chunk = miniChunk ? ReadMiniChunk() : ReadChunk();
        return 8 + Skip(chunk.Size);
    }

    public string ReadString(int size, Encoding encoding, bool zeroTerminated)
    {
        return _binaryReader.ReadString(size, encoding, zeroTerminated);
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