﻿using System;
using System.IO.Abstractions;
using System.Threading;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Mods;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps.Build;

internal class CleanOutdatedAssetsStep(IPhysicalMod mod, IServiceProvider serviceProvider)
    : PipelineStep(serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    protected override void RunCore(CancellationToken token)
    {
        var matcher = new Matcher();
        matcher.AddInclude("Data/Audio/SFX/sfx2d_*.meg");

        foreach (var fileToDelete in matcher.GetResultsInFullPath(mod.Directory.FullName))
        {
            Logger?.LogDebug($"Deleting old asset '{fileToDelete}'");
            _fileSystem.File.DeleteWithRetry(fileToDelete);
        }
    }
}