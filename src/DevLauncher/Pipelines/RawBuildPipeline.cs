using System;
using System.Collections.Generic;
using AnakinRaW.CommonUtilities.SimplePipeline;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Configuration;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines.Steps;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class RawBuildPipeline(RaWBuildOption buildOption, IPhysicalMod republicAtWar, IServiceProvider serviceProvider)
    : SequentialPipeline(serviceProvider)
{
    private readonly IPhysicalMod _republicAtWar = republicAtWar ?? throw new ArgumentNullException(nameof(republicAtWar));

    protected override IList<IStep> BuildStepsOrdered()
    {
        return new List<IStep>
        {
            new PackMegFileStep(new RawAiPackMegConfiguration(_republicAtWar, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawCustomMapsPackMegConfiguration(_republicAtWar, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawEnglishSFXMegConfiguration(_republicAtWar, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawGermanSFXMegConfiguration(_republicAtWar, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawNonLocalizedSFXMegConfiguration(_republicAtWar, ServiceProvider), ServiceProvider),

            new PackIconsStep(buildOption, ServiceProvider),

            new CompileLocalizationStep(ServiceProvider),
        };
    }

    public override string ToString()
    {
        return "Build Republic at War";
    }
}