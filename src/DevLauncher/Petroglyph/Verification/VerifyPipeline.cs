using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using PG.StarWarsGame.Infrastructure;
using PG.StarWarsGame.Infrastructure.Games;
using RepublicAtWar.DevLauncher.Petroglyph.Engine;
using RepublicAtWar.DevLauncher.Petroglyph.Engine.Pipeline;
using RepublicAtWar.DevLauncher.Petroglyph.Verification.Steps;

namespace RepublicAtWar.DevLauncher.Petroglyph.Verification;

public class VerifyPipeline(IPhysicalPlayableObject playableObject, IGame empireAtWarFallback, IServiceProvider serviceProvider)
    : ParallelPipeline(serviceProvider, 4, false)
{
    private IList<GameVerificationStep> _verificationSteps = null!;

    protected override Task<IList<IStep>> BuildSteps()
    {
        var repository = new GameRepository(playableObject, empireAtWarFallback, ServiceProvider);

        var buildIndexStep = new CreateGameDatabaseStep(playableObject, empireAtWarFallback, repository, ServiceProvider);
        _verificationSteps = new List<GameVerificationStep>
        {
            new VerifyReferencedModelsStep(buildIndexStep, repository, ServiceProvider),
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
            throw new GameVerificationException(stepsWithVerificationErrors);
    }
}