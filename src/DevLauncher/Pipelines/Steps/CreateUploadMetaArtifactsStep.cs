using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using EawModinfo.Model;
using EawModinfo.Spec;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Utilities;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps;

internal class CreateUploadMetaArtifactsStep(IServiceProvider serviceProvider) : SynchronizedStep(serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(CreateUploadMetaArtifactsStep));

    internal string? SteamTitle { get; private set; }

    internal string? SteamJsonName { get; private set; }

    protected override void SynchronizedInvoke(CancellationToken token)
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

        SteamTitle = combined.SteamData.Title;
        SteamJsonName = $"{SteamTitle}.workshop.json";

        _fileSystem.File.WriteAllText("modinfo.json", combined.ToJson(true));
        _fileSystem.File.WriteAllText(SteamJsonName, combined.SteamData.ToJson(true));

        _logger?.LogInformation("Finish build release artifacts");
    }
}

internal class CopyReleaseStep : PipelineStep
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger? _logger;
    private readonly CreateUploadMetaArtifactsStep _buildArtifactsStep;
    private readonly ReleaseRepublicAtWarOption _releaseOptions;

    private readonly Matcher _fileCopyBlacklist;

    public CopyReleaseStep(CreateUploadMetaArtifactsStep buildArtifactsStep, ReleaseRepublicAtWarOption releaseOptions, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _buildArtifactsStep = buildArtifactsStep;
        _releaseOptions = releaseOptions;
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(CreateUploadMetaArtifactsStep));

        _fileCopyBlacklist = CreateBlacklist();
    }

    private Matcher CreateBlacklist()
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddExclude("/.git/**/*.*");
        matcher.AddExclude("/lib/**/*.*");
        matcher.AddExclude("/Data/CUSTOMMAPS/**/*.*");
        matcher.AddExclude("/Data/Art/Textures/Icons/**/*.*");
        matcher.AddExclude("/Data/Art/Textures/MT_CommandBar/**/*.*");
        matcher.AddExclude("/Data/Audio/Units/**/*.*");
        
        matcher.AddExclude("/Data/Text/**/*.txt");
        matcher.AddExclude("/Data/XML/DataMiner.exe");

        // Individual Items
        matcher.AddInclude("/Raw.ico");
        matcher.AddInclude("/modinfo.json");
        matcher.AddInclude("/credits.md");
        matcher.AddInclude("/splash.png");
        matcher.AddInclude("/Republic at War Team.md");
        matcher.AddInclude("/Republic at War Version History.md");

        // Everything else in Data that was not excluded
        matcher.AddInclude("/Data/**/*.*");

        return matcher;
    }

    protected override void RunCore(CancellationToken token)
    {
        _buildArtifactsStep.Wait();

        _logger?.LogInformation("Copying Release to SteamUploader ...");

        if (!_fileSystem.Directory.Exists(_releaseOptions.UploaderDirectory))
            throw new DirectoryNotFoundException("Unable to find SteamUploader directory");

        if (!_fileSystem.File.Exists(_fileSystem.Path.Combine(_releaseOptions.UploaderDirectory, "SteamWorkshopUploader.exe")) 
            || !_fileSystem.Directory.Exists(_fileSystem.Path.Combine(_releaseOptions.UploaderDirectory, "WorkshopContent")))
            throw new ArgumentException("The specified uploader directory is not valid.");


        var source = _fileSystem.Path.GetFullPath(".");

        var uploaderWsContentPath = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(_releaseOptions.UploaderDirectory,
            "WorkshopContent"));

        var assetCopyPath = _fileSystem.Path.Combine(uploaderWsContentPath, _buildArtifactsStep.SteamTitle);

        // Clean copy!
        _fileSystem.Directory.DeleteWithRetry(assetCopyPath);

        var steamJsonFile = _buildArtifactsStep.SteamJsonName;
        _fileSystem.File.Copy(steamJsonFile, _fileSystem.Path.Combine(uploaderWsContentPath, steamJsonFile), true);

        var progressBar = new ProgressBar();

        Task.Run(async () =>
            {
                await new DirectoryCopier(_fileSystem).CopyDirectoryAsync(source, assetCopyPath, progressBar, ShallCopyFile, 4,
                    token);
            }, default)
            .Wait(token);

        progressBar.Dispose();
        
        _logger?.LogInformation($"Copied assets to SteamUploader at '{assetCopyPath}'");
    }

    private bool ShallCopyFile(string fileToCopy)
    {
        var currentDirLength = Environment.CurrentDirectory.Length;
        var localPath = fileToCopy.Substring(currentDirLength + 1); 
        return _fileCopyBlacklist.Match(localPath).HasMatches;
    }
}