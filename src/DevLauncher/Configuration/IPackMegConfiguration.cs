using System.Collections.Generic;
using System.IO.Abstractions;

namespace RepublicAtWar.DevLauncher.Configuration;

internal interface IPackMegConfiguration
{
    IEnumerable<string> FilesToPack { get; }

    string? BaseMegFile { get; }

    public string FileName { get; }

    public bool FileNamesOnly { get; }

    public IDirectoryInfo VirtualRootDirectory { get; }
}