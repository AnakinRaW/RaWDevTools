using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevTools.Steps.Build.Meg;
using RepublicAtWar.DevTools.Steps.Build.Meg.Config;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.MegCompile;

internal class PackSfxMegPipeline(IPhysicalMod mod, BuildSettings settings, IServiceProvider serviceProvider) : SimplePipeline<ParallelRunner>(serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    protected override ParallelRunner CreateRunner()
    {
        return new ParallelRunner(2, ServiceProvider);
    }

    protected override Task<IList<IStep>> BuildSteps()
    {
        var languageManager = ServiceProvider.GetRequiredService<IGameLanguageManagerProvider>()
            .GetLanguageManager(GameEngineType.Foc);

        IList<IStep> steps = new List<IStep>();
        foreach (var focLanguage in languageManager.SupportedLanguages)
        {
            var isRaWSupported = IsSupportedByRaw(focLanguage);

            // There is no need to build non-supported languages if we don't do a release or force a clean build
            if (!isRaWSupported)
                continue;

            steps.Add(new PackMegFileStep(
                new RawLocalizedSFX2DMegConfiguration(focLanguage, isRaWSupported, mod, ServiceProvider), settings,
                ServiceProvider));
        }

        return Task.FromResult(steps);
    }

    private bool IsSupportedByRaw(LanguageType focLanguage)
    {
        var path = _fileSystem.Path.Combine(mod.Directory.FullName, "Data/Audio/Units", focLanguage.ToString());
        return _fileSystem.Directory.Exists(path);
    }
}