using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PG.Commons.Binary;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Files;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files;

internal class ModelFileReader(string fileName, ChunkReader reader, ChunkMetadata firstChunk) : ChunkFileReaderBase<AlamoModel>(fileName, reader, firstChunk)
{
    public override AlamoModel ReadFile()
    {
        var textures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var shaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var proxies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bones = new List<string>();

        ChunkMetadata? chunk = FirstChunk;
        while (chunk != null)
        {
            switch (chunk.Value.Type)
            {
                case (int)ChunkType.Skeleton:
                    ReadSkeleton(chunk.Value.Size, bones);
                    break;
                case (int)ChunkType.Mesh:
                    ReadMesh(chunk.Value.Size, textures, shaders);
                    break;
                case (int)ChunkType.Connections:
                    ReadConnections(chunk.Value.Size, proxies);
                    break;
                default:
                    ChunkReader.Skip(chunk.Value.Size);
                    break;
            }

            chunk = ChunkReader.TryReadChunk();
        }

        return new AlamoModel
        {
            Bones = bones,
            FileName = FileName,
            Textures = textures, 
            Shaders = shaders,
            Proxies = proxies
        };
    }

    private void ReadConnections(int size, HashSet<string> proxies)
    {
        var actualSize = 0;
        do
        {
            var chunk = ChunkReader.ReadChunk();
            actualSize += 8;

            if (chunk.Type == (int)ChunkType.ProxyConnection)
            {
                actualSize += ReadProxy(chunk.Size, out string proxy);
                if (proxy is null)
                    throw new InvalidOperationException("Proxy without name.");
                proxies.Add(proxy);
            }
            else
                actualSize += ChunkReader.Skip(chunk.Size);


        } while (actualSize < size);

        if (size != actualSize)
            throw new BinaryCorruptedException("Unable to read alo model.");
    }

    private int ReadProxy(int size, out string proxyName)
    {
        var actualSize = 0;
        proxyName = null!;
        do
        {
            var chunk = ChunkReader.ReadMiniChunk();
            actualSize += 2;

            if (chunk.Type == 5)
            {
                proxyName = ChunkReader.ReadString(chunk.Size, Encoding.ASCII, true);
                actualSize += chunk.Size;
            }
            else
                actualSize += ChunkReader.Skip(chunk.Size);


        } while (actualSize < size);

        if (size != actualSize)
            throw new BinaryCorruptedException("Unable to read alo model.");

        return size;
    }

    private void ReadMesh(int size, ISet<string> textures, ISet<string> shaders)
    {
        var actualSize = 0;
        do
        {
            var chunk = ChunkReader.ReadChunk();
            actualSize += 8;

            if (chunk.Type == (int)ChunkType.SubMeshMaterialInformation)
                actualSize += ReadSubMeshMaterialInformation(chunk.Size, textures, shaders);
            else
                actualSize += ChunkReader.Skip(chunk.Size);


        } while (actualSize < size);

        if (size != actualSize)
            throw new BinaryCorruptedException("Unable to read alo model.");
    }

    private int ReadSubMeshMaterialInformation(int size, ISet<string> textures, ISet<string> shaders)
    {
        var actualSize = 0;
        do
        {
            var chunk = ChunkReader.ReadChunk();
            actualSize += 8;

            switch (chunk.Type)
            {
                case (int)ChunkType.ShaderFileName:
                {
                    var shader = ChunkReader.ReadString(chunk.Size, Encoding.ASCII, true);
                    shaders.Add(shader);
                    actualSize += chunk.Size;
                    break;
                }
                case (int)ChunkType.ShaderTexture:
                    actualSize += ReadShaderTexture(chunk.Size, textures);
                    break;
                default:
                    actualSize += ChunkReader.Skip(chunk.Size);
                    break;
            }


        } while (actualSize < size);

        if (size != actualSize)
            throw new BinaryCorruptedException("Unable to read alo model.");

        return size;
    }

    private int ReadShaderTexture(int size, ISet<string> textures)
    {
        var actualTextureChunkSize = 0;
        do
        {
            var mini = ChunkReader.ReadMiniChunk();
            actualTextureChunkSize += 2;

            if (mini.Type == 2)
            {
                var texture = ChunkReader.ReadString(mini.Size, Encoding.ASCII, true);
                textures.Add(texture);
                actualTextureChunkSize += mini.Size;
            }
            else
                actualTextureChunkSize += ChunkReader.Skip(mini.Size);

        } while (actualTextureChunkSize != size);

        return size;
    }

    private void ReadSkeleton(int size, IList<string> bones)
    {
        var actualSize = 0;
       
        var boneCountChunk = ChunkReader.ReadChunk();
        actualSize += 8;

        Debug.Assert(boneCountChunk is { Size: 128, Type: (int)ChunkType.BoneCount });

        var boneCount = ChunkReader.ReadDword();
        actualSize += sizeof(uint);
        
        actualSize += ChunkReader.Skip(128 - sizeof(uint));

        for (var i = 0; i < boneCount; i++)
        { 
            var bone = ChunkReader.ReadChunk();
            actualSize += 8;

            Debug.Assert(bone is { Type: (int)ChunkType.Bone, IsContainer: true });

            var boneReadSize = 0;

            while (boneReadSize < bone.Size)
            {
                var innerBoneChunk = ChunkReader.ReadChunk();
                boneReadSize += 8;

                if (innerBoneChunk.Type == (int)ChunkType.BoneName)
                {
                    var nameSize = innerBoneChunk.Size;

                    var name = ChunkReader.ReadString(nameSize, Encoding.ASCII, true);
                    boneReadSize += nameSize;
                    bones.Add(name);
                }
                else
                {
                    boneReadSize += ChunkReader.Skip(innerBoneChunk.Size);
                }
            }

            actualSize += boneReadSize;
        }

        if (size != actualSize)
            throw new BinaryCorruptedException("Unable to read alo model.");
    }
}