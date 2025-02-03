using CommandLine;

namespace RepublicAtWar.DevLauncher.Options;

internal abstract class RaWBuildOption : DevToolsOptionBase
{
    [Option("cleanBuild")]
    public virtual bool CleanBuild { get; init; }
}