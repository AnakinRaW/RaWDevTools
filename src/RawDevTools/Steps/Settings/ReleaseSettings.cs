namespace RepublicAtWar.DevTools.PipelineSteps.Settings;

public class ReleaseSettings : PipelineSettingsBase
{
    public required string UploaderDirectory { get; init; }
}