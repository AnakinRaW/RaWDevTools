using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Configuration;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines.Steps;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class RawBuildPipeline(RaWBuildOption buildOption, IPhysicalMod republicAtWar, IServiceProvider serviceProvider)
    : ParallelPipeline(serviceProvider)
{
    public override string ToString()
    {
        return "Build Republic at War";
    }

    protected override Task<IList<IStep>> BuildSteps()
    {
        return Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new PackMegFileStep(new RawAiPackMegConfiguration(republicAtWar, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawCustomMapsPackMegConfiguration(republicAtWar, ServiceProvider),
                ServiceProvider),
            new PackMegFileStep(new RawEnglishSFXMegConfiguration(republicAtWar, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawGermanSFXMegConfiguration(republicAtWar, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawNonLocalizedSFXMegConfiguration(republicAtWar, ServiceProvider),
                ServiceProvider),

            new PackIconsStep(buildOption, ServiceProvider),

            new CompileLocalizationStep(ServiceProvider),
        });
    }
}