using System;
using System.Collections.Generic;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines.Steps;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class ReleaseRawPipeline : SequentialPipeline
{
    private readonly ILogger? _logger;

    private readonly ReleaseRepublicAtWarOption _options;
    private readonly IPhysicalMod _republicAtWar;
    private readonly IServiceProvider _serviceProvider;

    public ReleaseRawPipeline(ReleaseRepublicAtWarOption options, IPhysicalMod republicAtWar, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _options = options;
        _republicAtWar = republicAtWar ?? throw new ArgumentNullException(nameof(republicAtWar));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    protected override IList<IStep> BuildStepsOrdered()
    {
        var createArtifactStep = new CreateUploadMetaArtifactsStep(_serviceProvider);
        return new List<IStep>
        {
            new RunPipelineStep(new VerifyPipeline(_options, _republicAtWar, _serviceProvider), _serviceProvider),
            new RunPipelineStep(new RawBuildPipeline(_options, _republicAtWar, _serviceProvider), _serviceProvider),
            createArtifactStep,
            new CopyReleaseStep(createArtifactStep, _options, _serviceProvider),
        };
    }

    protected override void OnRunning(StepRunner buildRunner)
    {
        _logger?.LogInformation("Release Republic at War");
        base.OnRunning(buildRunner);
    }
}