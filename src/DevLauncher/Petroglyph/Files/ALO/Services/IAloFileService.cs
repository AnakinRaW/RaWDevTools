using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using PG.Commons.Files;
using RepublicAtWar.DevLauncher.Petroglyph.Files.ALO.Data;
using RepublicAtWar.DevLauncher.Petroglyph.Files.ChunkFiles;

namespace RepublicAtWar.DevLauncher.Petroglyph.Files.ALO.Services;

public interface IAloFileService
{
    IAloFile<IAloDataContent, PetroglyphFileInformation> Load(Stream stream, AloLoadOptions loadOptions = AloLoadOptions.Full);

    IAloModelFile LoadModel(Stream stream, AloLoadOptions loadOptions = AloLoadOptions.Full);

    IAloParticleFile LoadParticle(Stream stream, AloLoadOptions loadOptions = AloLoadOptions.Full);
}

public class AloFileService : IAloFileService
{
    public IAloFile<IAloDataContent, PetroglyphFileInformation> Load(Stream stream, AloLoadOptions loadOptions = AloLoadOptions.Full)
    {
        throw new NotImplementedException();
    }

    public IAloModelFile LoadModel(Stream stream, AloLoadOptions loadOptions = AloLoadOptions.Full)
    {
        throw new NotImplementedException();
    }

    public IAloParticleFile LoadParticle(Stream stream, AloLoadOptions loadOptions = AloLoadOptions.Full)
    {
        throw new NotImplementedException();
    }
}

public interface IChunkFile<out T, out TFileInfo> : IPetroglyphFileHolder<T, TFileInfo> where T : IChunkData where TFileInfo : PetroglyphFileInformation;

public interface IAloFile<out T, out TFileInfo> : IChunkFile<T, TFileInfo> where T : IAloDataContent where TFileInfo : PetroglyphFileInformation;


public interface IAloModelFile : IAloFile<AlamoModel, AloModelFileInformation>;

public interface IAloParticleFile : IAloFile<AlamoParticle, AloParticleFileInformation>;

public sealed class AloModelFile(
    AlamoModel model,
    AloModelFileInformation fileInformation,
    IServiceProvider serviceProvider)
    : PetroglyphFileHolder<AlamoModel, AloModelFileInformation>(model, fileInformation, serviceProvider), IAloModelFile
{

}

public sealed record AloModelFileInformation : PetroglyphFileInformation
{
    /// <summary>
    /// Gets the model's file version.
    /// </summary>
    public AloModelVersion FileVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AloModelFileInformation"/> class.
    /// </summary>
    /// <param name="path">The file path of the alo file.</param>
    /// <param name="fileVersion">The file version of the MEG file.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    [SetsRequiredMembers]
    public AloModelFileInformation(string path, AloModelVersion fileVersion) : base(path)
    {
        FileVersion = fileVersion;
    }
}

public sealed record AloParticleFileInformation : PetroglyphFileInformation
{
    /// <summary>
    /// Gets the model's file version.
    /// </summary>
    public AloModelVersion FileVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AloModelFileInformation"/> class.
    /// </summary>
    /// <param name="path">The file path of the alo file.</param>
    /// <param name="fileVersion">The file version of the MEG file.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    [SetsRequiredMembers]
    public AloParticleFileInformation(string path, AloModelVersion fileVersion) : base(path)
    {
        FileVersion = fileVersion;
    }
}


public interface IAloDataContent : IChunkData;


public enum AloModelVersion
{
    V1,
    V2
}


[Flags]
public enum AloLoadOptions
{
    /// <summary>
    /// Loads the entire file.
    /// </summary>
    /// <remarks>
    /// This option can be used if the file shall be rendered.
    /// </remarks>
    Full = 0,
    /// <summary>
    ///  Extracts only assets from the model/particle (which are textures and proxies)
    /// </summary>
    Assets = 1,
    /// <summary>
    /// If the file is a model, this option gets the list of bones from the model.
    /// </summary>
    Bones = 2,
}