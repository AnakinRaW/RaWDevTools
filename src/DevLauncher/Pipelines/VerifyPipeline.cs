using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines.Steps;
using RepublicAtWar.DevLauncher.Pipelines.Steps.Verification;

namespace RepublicAtWar.DevLauncher.Pipelines;

public class VerifyPipeline(DevToolsOptionBase option, IPhysicalMod republicAtWar, IGame empireAtWarFallback, IServiceProvider serviceProvider)
    : ParallelPipeline(serviceProvider, 4, false)
{
    private IList<ModVerificationStep> _verificationSteps = null!;

    protected override Task<IList<IStep>> BuildSteps()
    {
        var buildIndexStep = new IndexAssetsAndCodeStep(republicAtWar, empireAtWarFallback, option, ServiceProvider);
        _verificationSteps = new List<ModVerificationStep>
        {
            new VerifyModelsTexturesAndShadersSteps(buildIndexStep, ServiceProvider),
        };

        var allSteps = new List<IStep>
        {
            buildIndexStep
        };
        allSteps.AddRange(_verificationSteps);

        return Task.FromResult<IList<IStep>>(allSteps);
    }

    public override async Task RunAsync(CancellationToken token = default)
    {
        await base.RunAsync(token).ConfigureAwait(false);

        var stepsWithVerificationErrors = _verificationSteps.Where(x => x.VerifyErrors.Any()).ToList();
        if (stepsWithVerificationErrors.Any())
            throw new ModVerificationException(stepsWithVerificationErrors);
    }
}