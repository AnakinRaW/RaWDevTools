using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.Logging;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps;

internal class RunPipelineStep(IPipeline pipeline, IServiceProvider serviceProvider) : PipelineStep(serviceProvider)
{
    private readonly IPipeline _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

    protected override void RunCore(CancellationToken token)
    {
        Logger?.LogInformation($"Running {_pipeline}...");
        _pipeline.Run(token);
        Logger?.LogInformation($"Finished {_pipeline}");
    }

    protected override void Dispose(bool disposing)
    {
        _pipeline.Dispose();
    }
}