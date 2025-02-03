using CommandLine;

namespace RepublicAtWar.DevLauncher.Options;

[Verb("release")]
internal sealed class ReleaseRepublicAtWarOption : RaWBuildOption
{
    public override bool CleanBuild { get; init; } = true;

    public override bool WarnAsError { get; init; } = true;

    [Option("uploaderDir", Required = true)]
    public string UploaderDirectory { get; init; }
}