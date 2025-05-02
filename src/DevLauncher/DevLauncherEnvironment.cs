using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reflection;
using AnakinRaW.ApplicationBase;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.AppUpdaterFramework.Configuration;

namespace RepublicAtWar.DevLauncher;

internal class DevLauncherEnvironment(Assembly assembly, IFileSystem fileSystem) : UpdatableApplicationEnvironment(assembly, fileSystem)
{
    private const string ToolPathName = "RawDevLauncher";

    public override string ApplicationName => "Republic at War DevLauncher";
    
    public override Uri? RepositoryUrl => null;

    public override ICollection<Uri> UpdateMirrors { get; } = new List<Uri>
    {
        new($"https://republicatwar.com/downloads/{ToolPathName}")
    };
    public override string UpdateRegistryPath => $@"SOFTWARE\{ToolPathName}\Update";
    
    protected override string ApplicationLocalDirectoryName => ToolPathName;

    protected override UpdateConfiguration CreateUpdateConfiguration()
    {
        return new()
        {
            DownloadLocation = FileSystem.Path.Combine(ApplicationLocalPath, "downloads"),
            BackupLocation = FileSystem.Path.Combine(ApplicationLocalPath, "backups"),
            BackupPolicy = BackupPolicy.Required,
#if NETFRAMEWORK
            RestartConfiguration = new UpdateRestartConfiguration
            {
                SupportsRestart = true,
                PassCurrentArgumentsForRestart = true
            }
#endif
        };
    }
}