using System;
using System.Collections.Generic;
using AET.ModVerify;
using AET.ModVerify.Steps;
using Microsoft.Extensions.DependencyInjection;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Engine.Database;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class RawVerifyPipeline(
    GameEngineType targetType,
    GameLocations gameLocations,
    VerificationSettings settings,
    IServiceProvider serviceProvider)
    : VerifyGamePipeline(targetType, gameLocations, settings, serviceProvider)
{
    protected override IEnumerable<GameVerificationStep> CreateVerificationSteps(IGameDatabase database)
    {
        var provider = ServiceProvider.GetRequiredService<IVerificationProvider>();
        foreach (var verifier in provider.GetAllDefaultVerifiers(database, Settings))
        {
            yield return verifier;
        }
    }
}