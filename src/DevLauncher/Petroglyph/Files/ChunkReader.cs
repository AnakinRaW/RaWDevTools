using System;
using System.IO;
using System.Text;
using AnakinRaW.CommonUtilities;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files;

internal class ChunkReader : DisposableObject
{
    private readonly BinaryReader _binaryReader;


    public ChunkReader(Stream stream, Encoding encoding, bool leaveOpen = true)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        _binaryReader = new BinaryReader(stream, encoding, leaveOpen);
    }

    protected override void DisposeManagedResources()
    {
        base.DisposeManagedResources();
        _binaryReader.Dispose();
    }
}