using System;
using System.Threading;
using AET.Modinfo.Model;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using PG.StarWarsGame.Infrastructure.Clients.Arguments;
using PG.StarWarsGame.Infrastructure.Clients.Arguments.GameArguments;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Pipelines.Settings;
using RepublicAtWar.DevLauncher.Services;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps;

internal class LaunchStep(LaunchSettings options, IPhysicalMod mod, IServiceProvider serviceProvider) : PipelineStep(serviceProvider)
{
    private readonly IPhysicalMod _mod = mod ?? throw new ArgumentNullException(nameof(mod));

    protected override void RunCore(CancellationToken token)
    {
        var launcher = new GameLauncher(options, _mod, Services);
        var args = CreateGameArgs();
        launcher.Launch(args);
    }

    private ArgumentCollection CreateGameArgs()
    {
        var gameArgsBuilder = new GameArgumentsBuilder();
        gameArgsBuilder
            .Add(new LanguageArgument(LanguageInfo.Default))
            .Add(new NoArtProcessArgument())
            .AddSingleMod(_mod);

        if (options.Windowed)
            gameArgsBuilder.Add(new WindowedArgument());

        if (options.Debug)
            gameArgsBuilder.Add(new IgnoreAssertsArgument());

        return gameArgsBuilder.Build();
    }
}