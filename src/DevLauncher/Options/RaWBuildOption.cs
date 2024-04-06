using CommandLine;

namespace RepublicAtWar.DevLauncher.Options;

public abstract class RaWBuildOption : DevToolsOptionBase
{
    [Option("cleanBuild")]
    public virtual bool CleanBuild { get; init; }
}