using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reflection;
using AnakinRaW.ApplicationBase;
using AnakinRaW.AppUpdaterFramework.Configuration;

namespace RepublicAtWar.DevLauncher;

internal class DevLauncherEnvironment(Assembly assembly, IFileSystem fileSystem) : UpdatableApplicationEnvironment(assembly, fileSystem)
{
    private const string ToolPathName = "RawDevLauncher";

    public override string ApplicationName => "Republic at War DevLauncher";

    public override UpdateConfiguration UpdateConfiguration { get; }

    public override Uri? RepositoryUrl => null;

    public override ICollection<Uri> UpdateMirrors { get; } = new List<Uri>
    {
        new($"https://republicatwar.com/downloads/{ToolPathName}")
    };
    public override string UpdateRegistryPath => $@"SOFTWARE\{ToolPathName}";

    protected override string ApplicationLocalDirectoryName => ToolPathName;
}