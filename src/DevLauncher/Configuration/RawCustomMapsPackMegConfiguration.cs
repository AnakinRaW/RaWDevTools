using PetroGlyph.Games.EawFoc;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace RepublicAtWar.DevLauncher.Configuration;

internal class RawCustomMapsPackMegConfiguration : RawPackMegConfiguration
{
    public RawCustomMapsPackMegConfiguration(IPhysicalPlayableObject? physicalGameObject, IServiceProvider serviceProvider) 
        : base(physicalGameObject, serviceProvider)
    {
    }

    public override IEnumerable<IDirectoryInfo> InputDirectories => new List<IDirectoryInfo>
    {
        FileSystem.DirectoryInfo.New("Data/CUSTOMMAPS")
    };

    public override IEnumerable<IFileInfo> InputFiles => Enumerable.Empty<IFileInfo>();

    public override IFileInfo Output => FileSystem.FileInfo.New("Data/MPMaps.meg");

    public override bool IncludeSubDirectories => false;
}