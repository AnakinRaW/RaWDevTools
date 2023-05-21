using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PetroGlyph.Games.EawFoc;
using Validation;

namespace RepublicAtWar.DevLauncher.Configuration;

internal abstract class RawPackMegConfiguration : IPackMegConfiguration
{
    protected readonly IFileSystem FileSystem;

    public abstract IEnumerable<IDirectoryInfo> InputDirectories { get; }
    public abstract IEnumerable<IFileInfo> InputFiles { get; }
    public abstract IFileInfo Output { get; }
    public abstract bool IncludeSubDirectories { get; }

    public IDirectoryInfo? VirtualRootDirectory { get; }

    protected RawPackMegConfiguration(IPhysicalPlayableObject? physicalGameObject, IServiceProvider serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        FileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        VirtualRootDirectory = physicalGameObject?.Directory;
    }
}

internal class RawAiPackMegConfiguration : RawPackMegConfiguration
{
    public override IEnumerable<IFileInfo> InputFiles => Enumerable.Empty<IFileInfo>();

    public override IEnumerable<IDirectoryInfo> InputDirectories => new List<IDirectoryInfo>
    {
        FileSystem.DirectoryInfo.New("Data/Scripts/AI"),
        FileSystem.DirectoryInfo.New("Data/Xml/AI")
    };

    public override IFileInfo Output => FileSystem.FileInfo.New("Data/AIFiles.meg");

    public override bool IncludeSubDirectories => true;

    public RawAiPackMegConfiguration(IPhysicalPlayableObject? physicalGameObject, IServiceProvider serviceProvider)
        : base(physicalGameObject, serviceProvider)
    {
    }
}