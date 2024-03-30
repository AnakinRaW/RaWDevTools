﻿using System;
using System.Collections.Generic;
using AnakinRaW.CommonUtilities.SimplePipeline;
using PetroGlyph.Games.EawFoc.Mods;
using RepublicAtWar.DevLauncher.Configuration;
using RepublicAtWar.DevLauncher.Pipelines.Steps;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class RawBuildPipeline(IMod republicAtWar, IServiceProvider serviceProvider)
    : SequentialPipeline(serviceProvider)
{
    private readonly IMod _republicAtWar = republicAtWar ?? throw new ArgumentNullException(nameof(republicAtWar));

    protected override IList<IStep> BuildStepsOrdered()
    {
        if (_republicAtWar is not IPhysicalMod physicalRaw)
            throw new NotSupportedException("Mod must be physical!");

        return new List<IStep>
        {
            new PackMegFileStep(new RawAiPackMegConfiguration(physicalRaw, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawCustomMapsPackMegConfiguration(physicalRaw, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawEnglishSFXMegConfiguration(physicalRaw, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawGermanSFXMegConfiguration(physicalRaw, ServiceProvider), ServiceProvider),
            new PackMegFileStep(new RawNonLocalizedSFXMegConfiguration(physicalRaw, ServiceProvider), ServiceProvider),
        };
    }

    public override string ToString()
    {
        return "Build Republic at War";
    }
}