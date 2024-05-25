using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PG.Commons.Binary;
using RepublicAtWar.DevLauncher.Petroglyph.Files.ALO.Data;
using RepublicAtWar.DevLauncher.Petroglyph.Files.ChunkFiles;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files.ALO.Services;

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
            var chunk = ChunkReader.ReadChunk(ref actualSize);

            if (chunk.Type == (int)ChunkType.ProxyConnection)
            {
                ReadProxy(chunk.Size, out var proxy, ref actualSize);
                proxies.Add(proxy);
            }
            else
                ChunkReader.Skip(chunk.Size, ref actualSize);


        } while (actualSize < size);

        if (size != actualSize)
            throw new BinaryCorruptedException("Unable to read alo model.");
    }

    private void ReadProxy(int size, out string proxy, ref int readSize)
    {
        var actualSize = 0;
        proxy = null!;
        do
        {
            var chunk = ChunkReader.ReadMiniChunk(ref actualSize);

            if (chunk.Type == 5)
                proxy = ChunkReader.ReadString(chunk.Size, Encoding.ASCII, true, ref actualSize);
            else
                ChunkReader.Skip(chunk.Size, ref actualSize);

        } while (actualSize < size);

        if (size != actualSize)
            throw new BinaryCorruptedException("Unable to read alo model.");

        if (proxy is null)
            throw new BinaryCorruptedException("Alamo proxy does not have a name.");

        readSize += actualSize;
    }

    private void ReadMesh(int size, ISet<string> textures, ISet<string> shaders)
    {
        var actualSize = 0;
        do
        {
            var chunk = ChunkReader.ReadChunk(ref actualSize);

            if (chunk.Type == (int)ChunkType.SubMeshMaterialInformation)
                ReadSubMeshMaterialInformation(chunk.Size, textures, shaders, ref actualSize);
            else
                ChunkReader.Skip(chunk.Size, ref actualSize);


        } while (actualSize < size);

        if (size != actualSize)
            throw new BinaryCorruptedException("Unable to read alo model.");
    }

    private void ReadSubMeshMaterialInformation(int size, ISet<string> textures, ISet<string> shaders, ref int readSize)
    {
        var actualSize = 0;
        do
        {
            var chunk = ChunkReader.ReadChunk(ref actualSize);

            switch (chunk.Type)
            {
                case (int)ChunkType.ShaderFileName:
                    {
                        var shader = ChunkReader.ReadString(chunk.Size, Encoding.ASCII, true, ref actualSize);
                        shaders.Add(shader);
                        break;
                    }
                case (int)ChunkType.ShaderTexture:
                    ReadShaderTexture(chunk.Size, textures, ref actualSize);
                    break;
                default:
                    ChunkReader.Skip(chunk.Size, ref actualSize);
                    break;
            }


        } while (actualSize < size);

        if (size != actualSize)
            throw new BinaryCorruptedException("Unable to read alo model.");

        readSize += actualSize;
    }

    private void ReadShaderTexture(int size, ISet<string> textures, ref int readSize)
    {
        var actualTextureChunkSize = 0;
        do
        {
            var mini = ChunkReader.ReadMiniChunk(ref actualTextureChunkSize);

            if (mini.Type == 2)
            {
                var texture = ChunkReader.ReadString(mini.Size, Encoding.ASCII, true, ref actualTextureChunkSize);
                textures.Add(texture);
            }
            else
                ChunkReader.Skip(mini.Size, ref actualTextureChunkSize);

        } while (actualTextureChunkSize != size);

        readSize += actualTextureChunkSize;
    }

    private void ReadSkeleton(int size, IList<string> bones)
    {
        var actualSize = 0;

        var boneCountChunk = ChunkReader.ReadChunk(ref actualSize);

        Debug.Assert(boneCountChunk is { Size: 128, Type: (int)ChunkType.BoneCount });

        var boneCount = ChunkReader.ReadDword(ref actualSize);

        ChunkReader.Skip(128 - sizeof(uint), ref actualSize);

        for (var i = 0; i < boneCount; i++)
        {
            var bone = ChunkReader.ReadChunk(ref actualSize);

            Debug.Assert(bone is { Type: (int)ChunkType.Bone, IsContainer: true });

            var boneReadSize = 0;

            while (boneReadSize < bone.Size)
            {
                var innerBoneChunk = ChunkReader.ReadChunk(ref boneReadSize);

                if (innerBoneChunk.Type == (int)ChunkType.BoneName)
                {
                    var nameSize = innerBoneChunk.Size;

                    var name = ChunkReader.ReadString(nameSize, Encoding.ASCII, true, ref boneReadSize);
                    bones.Add(name);
                }
                else
                {
                    ChunkReader.Skip(innerBoneChunk.Size, ref boneReadSize);
                }
            }

            actualSize += boneReadSize;
        }

        if (size != actualSize)
            throw new BinaryCorruptedException("Unable to read alo model.");
    }
}