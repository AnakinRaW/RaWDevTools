using System;
using System.IO.Abstractions;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using EawModinfo.Model;
using EawModinfo.Spec;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps;

internal class CreateUploadMetaArtifactsStep(IServiceProvider serviceProvider) : PipelineStep(serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(CreateUploadMetaArtifactsStep));

    protected override void RunCore(CancellationToken token)
    {
        _logger?.LogInformation("Creating Modinfo, Steam json and splashes...");

        var baseInfo = ModinfoData.Parse(_fileSystem.File.ReadAllText("modinfo-base.json"));

        var version = baseInfo.Version;
        if (version is null)
            throw new InvalidOperationException("Modinfo base has not version set.");

        IModinfo releaseInfo;
        string steamDescription;
        if (version.IsPrerelease)
        {
            releaseInfo = ModinfoData.Parse(_fileSystem.File.ReadAllText("modinfo-beta.json"));
            steamDescription = _fileSystem.File.ReadAllText("SteamText-Beta.txt");
            _fileSystem.File.Copy("splash-beta.png", "splash.png", true);
        }
        else
        {
            releaseInfo = ModinfoData.Parse(_fileSystem.File.ReadAllText("modinfo-stable.json"));
            steamDescription = _fileSystem.File.ReadAllText("SteamText-Stable.txt");
            _fileSystem.File.Copy("splash-stable.png", "splash.png", true);
        }

        if (releaseInfo.SteamData is null)
            throw new InvalidOperationException("SteamData of release modinfo file must not be null");

        var steamDataWithDescription = new SteamData(releaseInfo.SteamData)
        {
            Description = steamDescription
        };

        var combined = new ModinfoData(baseInfo)
        {
            SteamData = steamDataWithDescription
        };

        _fileSystem.File.WriteAllText("modinfo.json", combined.ToJson(true));
        _fileSystem.File.WriteAllText($"{combined.SteamData.Title}.workshop.json", combined.SteamData.ToJson(true));

        _logger?.LogInformation("Finish build release artifacts");
    }
}