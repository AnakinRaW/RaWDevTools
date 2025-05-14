using CommandLine;

namespace RepublicAtWar.DevLauncher.Update;

internal sealed class SkipUpdateOption
{
    [Option("skipUpdate", Default = false, 
        HelpText = "When set, the application does not search or install updates (an incomplete update will be finalized).", Hidden = true)]
    public bool SkipUpdate { get; init; }
}