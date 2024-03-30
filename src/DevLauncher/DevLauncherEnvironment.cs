using System;
using System.Collections.Generic;
using System.Reflection;
using AnakinRaW.ApplicationBase;

namespace RepublicAtWar.DevLauncher;

internal class DevLauncherEnvironment(Assembly assembly, IServiceProvider serviceProvider)
    : ApplicationEnvironmentBase(assembly, serviceProvider)
{
    private const string ToolPathName = "RawDevLauncher";

    public override string ApplicationName => "Republic at War DevLauncher";
    public override Uri? RepositoryUrl => null;
    public override ICollection<Uri> UpdateMirrors { get; } = new List<Uri>
    {
        new($"https://republicatwar.com/downloads/{ToolPathName}")
    };
    public override string ApplicationRegistryPath => $@"SOFTWARE\{ToolPathName}";
    protected override string ApplicationLocalDirectoryName => ToolPathName;
}