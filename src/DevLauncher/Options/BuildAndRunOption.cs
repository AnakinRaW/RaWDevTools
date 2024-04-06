using CommandLine;

namespace RepublicAtWar.DevLauncher.Options;

[Verb("buildRun", true)]
public sealed class BuildAndRunOption : RaWBuildOption
{
    [Option('w', "windowed", Default = false)]
    public bool Windowed { get; init; }

    [Option("skipRun")]
    public bool SkipRun { get; init; }
}