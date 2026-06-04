using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Net;
using System.Reflection;

namespace RepublicAtWar.DevLauncher;

internal class DevLauncherEnvironment(Assembly assembly, IFileSystem fileSystem)
    : UpdatableApplicationEnvironment(assembly, fileSystem)
{
    private const string ToolPathName = "RawDevLauncher";

    public override string ApplicationName => "Republic at War DevLauncher";
    
    public override ICollection<Uri> UpdateMirrors { get; } = new List<Uri>
    {
        new($"https://republicatwar.com/downloads/{ToolPathName}/v2")
    };
    public override string UpdateRegistryPath => $@"SOFTWARE\{ToolPathName}\Update";
    
    protected override string ApplicationLocalDirectoryName => ToolPathName;

    static DevLauncherEnvironment()
    {
        // For some unknown reason, packaging dependencies into the app, may alter the used security protocols...
        // This reverts the changes and forces secure settings
        if (ServicePointManager.SecurityProtocol != SecurityProtocolType.SystemDefault)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault | SecurityProtocolType.Tls12;
    }

    protected override UpdateConfiguration CreateUpdateConfiguration()
    {
        return new()
        {
            DownloadLocation = FileSystem.Path.Combine(ApplicationLocalPath, "downloads"),
            BackupLocation = FileSystem.Path.Combine(ApplicationLocalPath, "backups"),
            BackupPolicy = BackupPolicy.Required,
            DownloadRetryCount = 3,
            RestartConfiguration = new UpdateRestartConfiguration
            {
                SupportsRestart = true,
                PassCurrentArgumentsForRestart = true
            },
            ManifestDownloadConfiguration = new ManifestDownloadConfiguration
            {
                DownloadRetryDelay = 500
            },
            ComponentDownloadConfiguration = new DownloadManagerConfiguration
            {
                ValidationPolicy = ValidationPolicy.Required
            },
            ValidateInstallation = true,
            ManifestSigningConfiguration = new SigningConfiguration
            {
                Policy = SignaturePolicy.Required,
                SignatureAlgorithm = SignatureAlgorithm.ES256
            }
        };
    }
}