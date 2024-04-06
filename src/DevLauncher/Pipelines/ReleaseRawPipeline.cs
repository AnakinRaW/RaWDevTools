using System;
using System.Collections.Generic;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines.Steps;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class ReleaseRawPipeline : Pipeline
{
    private readonly ILogger? _logger;

    private readonly ReleaseRepublicAtWarOption _options;
    private readonly IPhysicalMod _republicAtWar;
    private readonly IServiceProvider _serviceProvider;

    private readonly StepRunner _buildPipeline;

    public ReleaseRawPipeline(ReleaseRepublicAtWarOption options, IPhysicalMod republicAtWar, IServiceProvider serviceProvider)
    {
        _options = options;
        _republicAtWar = republicAtWar ?? throw new ArgumentNullException(nameof(republicAtWar));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _buildPipeline = new StepRunner(serviceProvider);

        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    protected override bool PrepareCore()
    {
        _buildPipeline.Queue(new RunPipelineStep(new RawBuildPipeline(_options, _republicAtWar, _serviceProvider), _serviceProvider));
        _buildPipeline.Queue(new CreateUploadMetaArtifactsStep(_serviceProvider));
        return true;
    }

    protected override void RunCore(CancellationToken token)
    {
        _logger?.LogInformation("Release Republic at War");
        _buildPipeline.Error += OnError;
        try
        {
            _buildPipeline.Run(token);
        }
        finally
        {
            _buildPipeline.Error -= OnError;
            _logger?.LogTrace("Completed Release pipeline.");
        }
    }

    private static void OnError(object sender, StepErrorEventArgs e)
    {
        throw new StepFailureException(new List<IStep> { e.Step });
    }
}