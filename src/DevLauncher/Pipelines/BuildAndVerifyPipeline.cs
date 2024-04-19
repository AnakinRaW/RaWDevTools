using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines.Steps;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class BuildAndVerifyPipeline(RaWBuildOption buildOption, IPhysicalMod republicAtWar, IGame fallbackGame, IServiceProvider serviceProvider)
    : SequentialPipeline(serviceProvider)
{
    public override string ToString()
    {
        return "Build & Verify Republic at War";
    }

    protected override Task<IList<IStep>> BuildSteps()
    {
        return Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new RunPipelineStep(new RawBuildPipeline(buildOption, republicAtWar, ServiceProvider), ServiceProvider),
            new RunPipelineStep(new VerifyPipeline(buildOption, republicAtWar, fallbackGame, ServiceProvider), ServiceProvider),
        });
    }
}