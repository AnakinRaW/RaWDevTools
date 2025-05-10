using CommandLine;

namespace RepublicAtWar.DevLauncher.Options;

internal abstract class DevToolsOptionBase
{
    [Option("warnAsError")]
    public virtual bool WarnAsError { get; init; }
}