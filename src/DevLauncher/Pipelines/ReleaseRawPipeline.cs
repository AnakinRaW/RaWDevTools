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
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines.Steps.Release;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class ReleaseRawPipeline : SequentialPipeline
{
    private readonly ILogger? _logger;

    private readonly ReleaseRepublicAtWarOption _options;
    private readonly IPhysicalMod _republicAtWar;
    private readonly IGame _empireAtWarGame;

    public ReleaseRawPipeline(ReleaseRepublicAtWarOption options, IPhysicalMod republicAtWar, IGame empireAtWarGame, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _options = options;
        _republicAtWar = republicAtWar ?? throw new ArgumentNullException(nameof(republicAtWar));
        _empireAtWarGame = empireAtWarGame;

        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }
    protected override Task RunCoreAsync(CancellationToken token)
    {
        _logger?.LogInformation("Release Republic at War");
        return base.RunCoreAsync(token);
    }

    protected override Task<IList<IStep>> BuildSteps()
    {
        return Task.Run<IList<IStep>>(() =>
        {
            var createArtifactStep = new CreateUploadMetaArtifactsStep(ServiceProvider);
            return new List<IStep>
            {
                // Build
                new RunPipelineStep(new BuildPipeline(_republicAtWar, _options, ServiceProvider), ServiceProvider),
                // Verify
                // new RunPipelineStep(new VerifyPipeline(_options, _republicAtWar, _empireAtWarGame, ServiceProvider), ServiceProvider),
                // Build Release artifacts
                createArtifactStep,
                // Copy to Release
                new CopyReleaseStep(createArtifactStep, _options, ServiceProvider),
            };
        });
    }
}