﻿using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.DevTools.Steps.Releasing;

public class CopyReleaseStep : PipelineStep, IProgressStep
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger? _logger;
    private readonly CreateUploadMetaArtifactsStep _buildArtifactsStep;
    private readonly ReleaseSettings _settings;

    private readonly Matcher _fileCopyBlacklist;

    public ProgressType Type => CopyRelease;

    public long Size => 1;
    public event EventHandler<ProgressEventArgs<object?>>? Progress;

    private static readonly ProgressType CopyRelease = new()
    {
        Id = "copyRelease",
        DisplayName = "Copy Release"
    };

    public CopyReleaseStep(
        CreateUploadMetaArtifactsStep buildArtifactsStep,
        ReleaseSettings releaseOptions, 
        IServiceProvider serviceProvider) 
        : base(serviceProvider)
    {
        _buildArtifactsStep = buildArtifactsStep;
        _settings = releaseOptions;
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(CopyReleaseStep));
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

        if (!_fileSystem.Directory.Exists(_settings.UploaderDirectory))
            throw new DirectoryNotFoundException("Unable to find SteamUploader directory");

        if (!_fileSystem.File.Exists(_fileSystem.Path.Combine(_settings.UploaderDirectory, "SteamWorkshopUploader.exe"))
            || !_fileSystem.Directory.Exists(_fileSystem.Path.Combine(_settings.UploaderDirectory, "WorkshopContent")))
            throw new ArgumentException("The specified uploader directory is not valid.");


        var source = _fileSystem.Path.GetFullPath(".");

        var uploaderWsContentPath = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(_settings.UploaderDirectory, "WorkshopContent"));

        var assetCopyPath = _fileSystem.Path.Combine(uploaderWsContentPath, _buildArtifactsStep.SteamTitle);

        // Clean copy!
        _fileSystem.Directory.DeleteWithRetry(assetCopyPath);

        var steamJsonFile = _buildArtifactsStep.SteamJsonName;
        _fileSystem.File.Copy(steamJsonFile, _fileSystem.Path.Combine(uploaderWsContentPath, steamJsonFile), true);

        Task.Run(async () =>
            {
                await new DirectoryCopier(_fileSystem).CopyDirectoryAsync(source, 
                    assetCopyPath, 
                    new CopyProgress(this),
                    ShallCopyFile, 4,
                    token);
            }, CancellationToken.None)
            .Wait(token);
        
        _logger?.LogInformation($"Copied assets to SteamUploader at '{assetCopyPath}'");
    }

    private bool ShallCopyFile(string fileToCopy)
    {
        var currentDirLength = Environment.CurrentDirectory.Length;
        var localPath = fileToCopy.Substring(currentDirLength + 1);
        return _fileCopyBlacklist.Match(localPath).HasMatches;
    }

    private void OnProgress(double progress) 
    {
        Progress?.Invoke(this, new ProgressEventArgs<object?>(progress, null));
    }

    private class CopyProgress(CopyReleaseStep releaseStep) : IProgress<double>
    {
        public void Report(double value)
        {
            releaseStep.OnProgress(value);
        }
    }
}