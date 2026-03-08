using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.Logging;

namespace RepublicAtWar.DevTools.Steps;

public abstract class SingleActionPipeline(IServiceProvider serviceProvider, bool warningAsError) 
    : SequentialPipeline(serviceProvider)
{
   
    private class SimpleRunnerStep(Action<CancellationToken> action, IServiceProvider serviceProvider) 
        : PipelineStep(serviceProvider)
    {
        protected override Task RunCoreAsync(CancellationToken token)
        {
            return Task.Run(() => action(token), CancellationToken.None);
        }
    }

    protected override Task<IList<IStep>> CreateRunnerSteps(CancellationToken token)
    {
        return Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new SimpleRunnerStep(RunAction, ServiceProvider)
        });
    }

    protected abstract void RunAction(CancellationToken cancellationToken);

    protected void LogOrThrow(string message)
    {
        if (warningAsError)
            throw new InvalidOperationException(message);
        Logger?.LogWarning(message);
    }
}