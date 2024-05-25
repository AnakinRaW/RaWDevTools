using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Configuration;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines.Steps.Build;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class BuildPipeline(IPhysicalMod mod, RaWBuildOption buildOption, IServiceProvider serviceProvider)
    : ParallelPipeline(serviceProvider)
{
    public override string ToString()
    {
        return $"Building {mod.Name}";
    }

    protected override Task<IList<IStep>> BuildSteps()
    {
        return Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new PackMegFileStep(new RawAiPackMegConfiguration(mod, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawCustomMapsPackMegConfiguration(mod, ServiceProvider),
                ServiceProvider),
            new PackMegFileStep(new RawEnglishSFXMegConfiguration(mod, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawGermanSFXMegConfiguration(mod, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawNonLocalizedSFXMegConfiguration(mod, ServiceProvider),
                ServiceProvider),

            new PackIconsStep(buildOption, ServiceProvider),

            new CompileLocalizationStep(ServiceProvider),
        });
    }
}