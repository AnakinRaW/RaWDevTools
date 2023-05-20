using System;
using System.Collections.Generic;
using AnakinRaW.CommonUtilities.SimplePipeline;
using PetroGlyph.Games.EawFoc.Mods;
using RepublicAtWar.DevLauncher.Configuration;
using RepublicAtWar.DevLauncher.Pipelines.Steps;
using Validation;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class RawBuildPipeline : ParallelPipeline
{
    private readonly IMod _republicAtWar;

    public RawBuildPipeline(IMod republicAtWar, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Requires.NotNull(republicAtWar, nameof(republicAtWar));
        _republicAtWar = republicAtWar;
    }

    protected override IList<IStep> BuildStepsOrdered()
    {
        return new List<IStep>
        {
            new PackMegFileStep(new RawAiPackMegConfiguration(), ServiceProvider),
            new PackMegFileStep(new RawCustomMapsPackMegConfiguration(), ServiceProvider)
        };
    }

    public override string ToString()
    {
        return "Build Republic at War";
    }
}