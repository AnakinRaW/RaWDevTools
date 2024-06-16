using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;

namespace RepublicAtWar.DevLauncher.Pipelines.Actions;

internal abstract class LauncherAction(IServiceProvider serviceProvider) : SequentialPipeline(serviceProvider)
{
    protected override Task<IList<IStep>> BuildSteps()
    {
        return Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new SimpleRunnerStep(RunAction, ServiceProvider)
        });
    }

    private class SimpleRunnerStep(Action<CancellationToken> action, IServiceProvider serviceProvider) : PipelineStep(serviceProvider)
    {
        protected override void RunCore(CancellationToken token)
        {
            action(token);
        }
    }

    protected abstract void RunAction(CancellationToken cancellationToken);
}

internal class InitializeLocalizationAction(IServiceProvider serviceProvider) : LauncherAction(serviceProvider)
{
    protected override void RunAction(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

internal class CreateLocalizationDiffsAction(IServiceProvider serviceProvider) : LauncherAction(serviceProvider)
{
    protected override void RunAction(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

internal class MergeLocalizationsAction(IServiceProvider serviceProvider) : LauncherAction(serviceProvider)
{
    protected override void RunAction(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}