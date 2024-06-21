using CommandLine;

namespace RepublicAtWar.DevLauncher.Options;

[Verb("buildRun", true)]
internal sealed class BuildAndRunOption : RaWBuildOption
{
    [Option('w', "windowed", Default = false)]
    public bool Windowed { get; init; }

    [Option('d', "debug", Default = false)]
    public bool Debug { get; init; }

    [Option("skipRun")]
    public bool SkipRun { get; init; }
}