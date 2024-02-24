using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using PetroGlyph.Games.EawFoc;

namespace RepublicAtWar.DevLauncher.Configuration;

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