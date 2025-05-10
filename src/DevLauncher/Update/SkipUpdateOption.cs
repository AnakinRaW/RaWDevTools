using CommandLine;

namespace RepublicAtWar.DevLauncher.Update;

internal sealed class SkipUpdateOption
{
    [Option("skipUpdate", Default = false, HelpText = "Skips update procedure.", Hidden = true)]
    public bool SkipUpdate { get; init; }
}