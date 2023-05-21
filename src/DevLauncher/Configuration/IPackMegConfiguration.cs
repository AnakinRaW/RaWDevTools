using System.Collections.Generic;
using System.IO.Abstractions;

namespace RepublicAtWar.DevLauncher.Configuration;

internal interface IPackMegConfiguration
{
    public IEnumerable<IDirectoryInfo> InputDirectories { get; }

    public IEnumerable<IFileInfo> InputFiles { get; }

    public IFileInfo Output { get; }

    public bool IncludeSubDirectories { get; }

    public IDirectoryInfo? VirtualRootDirectory { get; }
}