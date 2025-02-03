using System;
using System.Collections.Generic;
using AET.ModVerify;
using AET.ModVerify.Settings;
using AET.ModVerify.Verifiers;
using Microsoft.Extensions.DependencyInjection;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Engine.Database;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class RawVerifyPipeline(
    GameEngineType targetType,
    GameLocations gameLocations,
    GameVerifySettings settings,
    IServiceProvider serviceProvider)
    : VerifyGamePipeline(targetType, gameLocations, settings, serviceProvider)
{
    protected override IEnumerable<GameVerifierBase> CreateVerificationSteps(IGameDatabase database)
    {
        var provider = ServiceProvider.GetRequiredService<IVerificationProvider>();
        foreach (var verifier in provider.GetAllDefaultVerifiers(database, Settings))
        {
            yield return verifier;
        }
        
    }
}