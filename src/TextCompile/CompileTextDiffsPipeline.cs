using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.TextCompile;

internal class CompileTextDiffsPipeline : SequentialPipeline
{
    private readonly BuildSettings _settings;

    public CompileTextDiffsPipeline(BuildSettings settings, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _settings = settings;
        FailFast = true;
    }

    protected override Task<IList<IStep>> CreateRunnerSteps(CancellationToken token)
    {
        IList<IStep> steps = new List<IStep>
        {
            new MergeDiffIntoDatStep(ServiceProvider, _settings)
        };
        return Task.FromResult(steps);
    }
}