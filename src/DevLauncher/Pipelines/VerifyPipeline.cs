using System;
using System.Collections.Generic;
using AnakinRaW.CommonUtilities.SimplePipeline;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines.Steps;

namespace RepublicAtWar.DevLauncher.Pipelines;

public class VerifyPipeline(DevToolsOptionBase option, IPhysicalMod republicAtWar, IGame empireAtWarFallback, IServiceProvider serviceProvider)
    : ParallelPipeline(serviceProvider, 4, false)
{
    protected override IList<IStep> BuildStepsOrdered()
    {
        var buildIndexStep = new IndexAssetsAndCodeStep(republicAtWar, empireAtWarFallback, option, ServiceProvider);

        return new List<IStep>
        {
            buildIndexStep
        };
    }
}