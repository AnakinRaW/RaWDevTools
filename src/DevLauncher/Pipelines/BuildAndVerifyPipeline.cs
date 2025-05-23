﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AET.ModVerify.Pipeline;
using AET.ModVerify.Pipeline.Progress;
using AET.ModVerify.Reporting;
using AET.ModVerify.Reporting.Settings;
using AET.ModVerify.Settings;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class BuildAndVerifyPipeline(IPhysicalMod mod, IGame fallbackGame, BuildSettings buildSettings, IServiceProvider serviceProvider)
    : SequentialPipeline(serviceProvider)
{
    public override string ToString()
    {
        return $"Build & Verify {mod.Name}";
    }

    protected override Task<IList<IStep>> BuildSteps()
    {
        return Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new RunPipelineStep(new BuildPipeline(mod, buildSettings, ServiceProvider), ServiceProvider),
            new RunPipelineStep(CreateVerifyPipeline(), ServiceProvider),
        });
    }

    private GameVerifyPipeline CreateVerifyPipeline()
    {
        var gameLocations = new GameLocations(
            [mod.Directory.FullName],
            mod.Game.Directory.FullName,
            [fallbackGame.Directory.FullName, LauncherConstants.RaWFallbackAssetPathEaW]
        );

        var settings = new VerifyPipelineSettings
        {
            GameVerifySettings = GameVerifySettings.Default,
            VerifiersProvider = new DefaultGameVerifiersProvider()
        };

        var globalReportSettings = new GlobalVerifyReportSettings
        {
            Baseline = VerificationBaseline.Empty,
            Suppressions = SuppressionList.Empty
        };

        var gameEngineService = ServiceProvider.GetRequiredService<IPetroglyphStarWarsGameEngineService>();

        var engineErrorReporter = new ConcurrentGameEngineErrorReporter();

        var gameEngine = gameEngineService.InitializeAsync(
            GameEngineType.Foc,
            gameLocations,
            engineErrorReporter,
            null,
            false,
            CancellationToken.None).GetAwaiter().GetResult();

        return new GameVerifyPipeline(gameEngine, engineErrorReporter, settings, globalReportSettings, new NullVerifyProgressReporter(), ServiceProvider);
    }

    private class NullVerifyProgressReporter : IVerifyProgressReporter
    {
        public void Report(double progress, string? progressText, ProgressType type, VerifyProgressInfo detailedProgress)
        {
            
        }
    }
}