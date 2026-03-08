using AET.Modinfo.Spec;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevTools.Services;
using RepublicAtWar.DevTools.Steps.Build.Meg;
using RepublicAtWar.DevTools.Steps.Build.Meg.Config;
using RepublicAtWar.DevTools.Steps.Settings;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RepublicAtWar.MegCompile;

internal class PackSfxMegPipeline(IPhysicalMod mod, BuildSettings settings, IServiceProvider serviceProvider) 
    : StepRunnerPipeline(serviceProvider)
{
    private readonly RepublicAtWarService _republicAtWarService = new(serviceProvider);

    protected override IStepRunner CreateRunner()
    {
        return new AsyncStepRunner(2, ServiceProvider);
    }

    protected override Task<IList<IStep>> CreateRunnerSteps(CancellationToken token)
    {
        IList<IStep> steps = new List<IStep>();
        foreach (var supportedLanguage in _republicAtWarService.GetSupportedLanguages())
        {
            var hasSfxSupport = supportedLanguage.support.HasFlag(LanguageSupportLevel.SFX);

            // There is no need to build non-supported languages if we don't do a release or force a clean build
            if (!hasSfxSupport)
                continue;

            steps.Add(new PackMegFileStep(
                new RawLocalizedSfx2DMegConfiguration(supportedLanguage.langauge, hasSfxSupport, mod, ServiceProvider), settings,
                ServiceProvider));
        }
        return Task.FromResult(steps);
    }
}