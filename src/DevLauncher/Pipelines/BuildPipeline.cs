using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using PG.StarWarsGame.Engine.Language;
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
        IList<IStep> steps = new List<IStep>
        {
            new PackMegFileStep(new RawAiPackMegConfiguration(mod, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawCustomMapsPackMegConfiguration(mod, ServiceProvider), ServiceProvider),

            new PackMegFileStep(new RawNonLocalizedSFXMegConfiguration(mod, ServiceProvider), ServiceProvider),
            new PackIconsStep(buildOption, ServiceProvider),
            new CompileLocalizationStep(ServiceProvider),
        };

        foreach (var supportedLanguage in GameLanguageManager.FocSupportedLanguages)
        {
            steps.Add(new PackMegFileStep(
                new RawLocalizedSFX2DMegConfiguration(supportedLanguage.ToString(), mod, ServiceProvider),
                ServiceProvider));
        }

        return Task.FromResult(steps);
    }
}