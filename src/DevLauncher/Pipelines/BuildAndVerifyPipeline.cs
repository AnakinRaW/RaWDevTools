using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AET.ModVerify.Settings;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class BuildAndVerifyPipeline(IPhysicalMod mod, IGame fallbackGame, BuildSettings buildSettings, IServiceProvider serviceProvider)
    : SequentialPipeline(serviceProvider)
{
    public override string ToString()
    {
        return $"Build & Verify {mod.Name}";
    }

    protected override Task<IList<IStep>> BuildSteps()
    {
        var gameLocations = new GameLocations(
            [mod.Directory.FullName],
            mod.Game.Directory.FullName,
            [fallbackGame.Directory.FullName, LauncherConstants.RaWFallbackAssetPathEaW]
        );

        return Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new RunPipelineStep(new BuildPipeline(mod, buildSettings, ServiceProvider), ServiceProvider),
            new RunPipelineStep(new RawVerifyPipeline(GameEngineType.Foc, gameLocations, GameVerifySettings.Default, ServiceProvider), ServiceProvider),
        });
    }
}