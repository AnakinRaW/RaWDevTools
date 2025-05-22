using CommandLine;

namespace RepublicAtWar.DevLauncher.Options;

internal class VerboseLoggingOption
{
    [Option('v', "verbose", Default = false, HelpText = "Enables verbose logging for this application.")]
    public bool VerboseLogging { get; init; }
}