using System;
using System.Collections.Generic;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Pipelines.Steps;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class RawDevLauncherPipeline : Pipeline
{
    private readonly ILogger? _logger;

    private readonly BuildAndRunOption _options;
    private readonly IMod _republicAtWar;
    private readonly IServiceProvider _serviceProvider;

    private readonly StepRunner _buildPipeline;

    public RawDevLauncherPipeline(BuildAndRunOption options, IMod republicAtWar, IServiceProvider serviceProvider)
    {
        _options = options;
        _republicAtWar = republicAtWar ?? throw new ArgumentNullException(nameof(republicAtWar));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _buildPipeline = new StepRunner(serviceProvider);

        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    protected override bool PrepareCore()
    {
        _buildPipeline.Queue(new RunPipelineStep(new RawBuildPipeline(_republicAtWar, _serviceProvider), _serviceProvider));
        _buildPipeline.Queue(new LaunchStep(_options, _republicAtWar, _serviceProvider));
        return true;
    }

    protected override void RunCore(CancellationToken token)
    {
        _logger?.LogTrace("Starting mod build pipeline.");
        _buildPipeline.Error += OnError;
        try
        {
            _buildPipeline.Run(token);
        }
        finally
        {
            _buildPipeline.Error -= OnError;
            _logger?.LogTrace("Completed build pipeline.");
        }
    }

    private static void OnError(object sender, StepErrorEventArgs e)
    {
        throw new StepFailureException(new List<IStep> { e.Step });
    }
}