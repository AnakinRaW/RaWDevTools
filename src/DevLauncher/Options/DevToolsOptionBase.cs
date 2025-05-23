﻿using CommandLine;

namespace RepublicAtWar.DevLauncher.Options;

internal abstract class DevToolsOptionBase
{
    [Option("warnAsError")]
    public virtual bool WarnAsError { get; init; }

    [Option('v', "verbose", Default = false, HelpText = "Enables verbose logging for this application.")]
    public bool VerboseLogging { get; init; }
}