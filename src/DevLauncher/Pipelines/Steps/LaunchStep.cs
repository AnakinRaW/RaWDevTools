using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using EawModinfo.Model;
using Microsoft.Extensions.DependencyInjection;
using PetroGlyph.Games.EawFoc.Clients.Arguments;
using PetroGlyph.Games.EawFoc.Clients.Arguments.GameArguments;
using PetroGlyph.Games.EawFoc.Mods;
using Validation;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps;

internal class LaunchStep : PipelineStep
{
    private readonly IMod _mod;

    public LaunchStep(IMod mod, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Requires.NotNull(mod, nameof(mod));
        _mod = mod;
    }

    protected override void RunCore(CancellationToken token)
    {
        var launcher = new GameLauncher(_mod, Services);
        var args = CreateGameArgs();
        launcher.Launch(args);
    }

    private IArgumentCollection CreateGameArgs()
    {
        var modArgFactory = Services.GetRequiredService<IModArgumentListFactory>();
        var modArgs = modArgFactory.BuildArgumentList(_mod, false);
        var gameArgsBuilder = Services.GetRequiredService<IArgumentCollectionBuilder>();
        gameArgsBuilder
            .Add(new WindowedArgument())
            .Add(new LanguageArgument(LanguageInfo.Default))
            .Add(modArgs);

        return gameArgsBuilder.Build();
    }
}