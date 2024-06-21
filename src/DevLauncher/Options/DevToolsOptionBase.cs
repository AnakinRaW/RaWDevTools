using AnakinRaW.ApplicationBase.Options;
using CommandLine;

namespace RepublicAtWar.DevLauncher.Options;

internal abstract class DevToolsOptionBase : UpdaterCommandLineOptions
{
    [Option("warnAsError")]
    public virtual bool WarnAsError { get; init; }
}