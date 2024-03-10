using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.Logging;
using Validation;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps;

internal class RunPipelineStep : PipelineStep
{
    private readonly IPipeline _pipeline;

    public RunPipelineStep(IPipeline pipeline, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Requires.NotNull(pipeline, nameof(pipeline));
        _pipeline = pipeline;
    }

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