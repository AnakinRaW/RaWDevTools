using AnakinRaW.ApplicationBase.Options;
using CommandLine;

namespace RepublicAtWar.DevLauncher.Options;

public abstract class DevToolsOptionBase : UpdaterCommandLineOptions
{
    [Option("warnAsError")]
    public virtual bool WarnAsError { get; init; }
}