using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.TextCompile;

internal class CompileTextDiffsPipeline(BuildSettings settings, IServiceProvider serviceProvider, bool failFast = true)
    : SequentialPipeline(serviceProvider, failFast)
{
    protected override Task<IList<IStep>> BuildSteps()
    {
        IList<IStep> steps = new List<IStep>
        {
            new MergeDiffIntoDatStep(ServiceProvider, settings)
        };
        return Task.FromResult(steps);
    }
}