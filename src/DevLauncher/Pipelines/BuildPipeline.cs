using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using Microsoft.Extensions.DependencyInjection;
using PG.StarWarsGame.Engine.Language;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Configuration;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines.Steps.Build;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class BuildPipeline(IPhysicalMod mod, RaWBuildOption buildOption, IServiceProvider serviceProvider)
    : ParallelPipeline(serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly IGameLanguageManager _languageManager = serviceProvider.GetRequiredService<IGameLanguageManager>();

    public override string ToString()
    {
        return $"Building {mod.Name}";
    }

    protected override Task<IList<IStep>> BuildSteps()
    {
        IList<IStep> steps = new List<IStep>
        {
            new PackMegFileStep(new RawAiPackMegConfiguration(mod, ServiceProvider), buildOption, ServiceProvider),
            new PackMegFileStep(new RawCustomMapsPackMegConfiguration(mod, ServiceProvider), buildOption, ServiceProvider),

            new PackMegFileStep(new RawNonLocalizedSFXMegConfiguration(mod, ServiceProvider), buildOption, ServiceProvider),
            new PackIconsStep(buildOption, ServiceProvider),
            new CompileLocalizationStep(ServiceProvider, buildOption)
        };


        foreach (var focLanguage in _languageManager.FocSupportedLanguages)
        {
            var isRaWSupported = IsSupportedByRaw(focLanguage);

            // There is no need to build non-supported languages if we don't do a release or force a clean build
            if (!isRaWSupported && !buildOption.CleanBuild)
                continue;

            steps.Add(new PackMegFileStep(
                new RawLocalizedSFX2DMegConfiguration(focLanguage, isRaWSupported, mod, ServiceProvider), 
                buildOption,
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