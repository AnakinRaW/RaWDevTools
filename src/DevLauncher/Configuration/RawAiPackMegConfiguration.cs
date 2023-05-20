using System.Collections.Generic;
using System.IO.Abstractions;

namespace RepublicAtWar.DevLauncher.Configuration;

internal class RawAiPackMegConfiguration : IPackMegConfiguration
{
    public IEnumerable<IFileInfo> InputLocations { get; }

    public IFileInfo Output { get; }

    public bool IncludeSubDirectories { get; }

    public string? WorkingDirectory { get; }
}