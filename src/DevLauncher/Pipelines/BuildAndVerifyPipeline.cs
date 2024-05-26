using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Petroglyph.Verification;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class BuildAndVerifyPipeline(RaWBuildOption buildOption, IPhysicalMod mod, IGame fallbackGame, IServiceProvider serviceProvider)
    : SequentialPipeline(serviceProvider)
{
    public override string ToString()
    {
        return $"Build & Verify {mod.Name}";
    }

    protected override Task<IList<IStep>> BuildSteps()
    {
        return Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new RunPipelineStep(new BuildPipeline(mod, buildOption, ServiceProvider), ServiceProvider),
            new RunPipelineStep(
                new VerifyFocPipeline(
                    [mod.Directory.FullName],
                    mod.Game.Directory.FullName,
                    fallbackGame.Directory.FullName,
                    ServiceProvider),
                ServiceProvider),
        });
    }
}