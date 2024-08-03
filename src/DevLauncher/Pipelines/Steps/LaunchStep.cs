using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using EawModinfo.Model;
using Microsoft.Extensions.DependencyInjection;
using PG.StarWarsGame.Infrastructure.Clients.Arguments;
using PG.StarWarsGame.Infrastructure.Clients.Arguments.GameArguments;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Pipelines.Settings;
using RepublicAtWar.DevLauncher.Services;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps;

internal class LaunchStep(LaunchSettings options, IMod mod, IServiceProvider serviceProvider) : PipelineStep(serviceProvider)
{
    private readonly IMod _mod = mod ?? throw new ArgumentNullException(nameof(mod));

    protected override void RunCore(CancellationToken token)
    {
        var launcher = new GameLauncher(options, _mod, Services);
        var args = CreateGameArgs();
        launcher.Launch(args);
    }

    private IArgumentCollection CreateGameArgs()
    {
        var modArgFactory = Services.GetRequiredService<IModArgumentListFactory>();
        var modArgs = modArgFactory.BuildArgumentList(_mod, false);
        var gameArgsBuilder = new UniqueArgumentCollectionBuilder();
        gameArgsBuilder
            .Add(new LanguageArgument(LanguageInfo.Default))
            .Add(new NoArtProcessArgument())
            .Add(modArgs);

        if (options.Windowed)
            gameArgsBuilder.Add(new WindowedArgument());

        if (options.Debug)
            gameArgsBuilder.Add(new IgnoreAssertsArgument());

        return gameArgsBuilder.Build();
    }
}