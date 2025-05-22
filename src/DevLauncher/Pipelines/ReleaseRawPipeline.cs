using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Utilities;
using RepublicAtWar.DevTools.Steps.Releasing;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class ReleaseRawPipeline : SequentialPipeline
{
    private readonly ILogger? _logger;
    private readonly BuildSettings _buildSettings;
    private readonly ReleaseSettings _releaseSettings;
    private readonly IPhysicalMod _republicAtWar;
    private readonly IGame _empireAtWarGame;

    private ProgressBarReporter? _progressBarReporter;

    public ReleaseRawPipeline(
        IPhysicalMod republicAtWar, 
        IGame empireAtWarGame,
        BuildSettings buildSettings,
        ReleaseSettings releaseSettings,
        IServiceProvider serviceProvider) 
        : base(serviceProvider)
    {
        _buildSettings = buildSettings ?? throw new ArgumentNullException(nameof(buildSettings));
        _releaseSettings = releaseSettings ?? throw new ArgumentNullException(nameof(releaseSettings));
        _republicAtWar = republicAtWar ?? throw new ArgumentNullException(nameof(republicAtWar));
        _empireAtWarGame = empireAtWarGame;
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }
    protected override Task RunCoreAsync(CancellationToken token)
    {
        _logger?.LogInformation("Release Republic at War");

        if (!_buildSettings.CleanBuild)
        {
            _logger?.LogWarning("Releasing without Clean build!!!");
            _logger?.LogWarning("Releasing without Clean build!!!");
            _logger?.LogWarning("Releasing without Clean build!!!");
        }
        return base.RunCoreAsync(token);
    }

    protected override void DisposeResources()
    {
        _progressBarReporter?.Dispose();
        _progressBarReporter = null;
        base.DisposeResources();
    }

    protected override Task<IList<IStep>> BuildSteps()
    {
        return Task.Run<IList<IStep>>(() =>
        {
            var createArtifactStep = new CreateUploadMetaArtifactsStep(ServiceProvider);
            
            var copyStep = new CopyReleaseStep(createArtifactStep, _releaseSettings, ServiceProvider);
            _progressBarReporter = new(copyStep);

            return new List<IStep>
            {
                // Build
                new RunPipelineStep(new BuildPipeline(_republicAtWar, _buildSettings, ServiceProvider), ServiceProvider),
                // Verify
                // new RunPipelineStep(new VerifyPipeline(_options, _republicAtWar, _empireAtWarGame, ServiceProvider), ServiceProvider),
                // Build Release artifacts
                createArtifactStep,
                // Copy to Release
                copyStep
            };
        });
    }
}