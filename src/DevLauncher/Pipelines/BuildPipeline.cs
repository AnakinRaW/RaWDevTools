using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Engine.Localization;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevTools.Steps.Build;
using RepublicAtWar.DevTools.Steps.Build.Meg;
using RepublicAtWar.DevTools.Steps.Build.Meg.Config;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal sealed class BuildPipeline : SequentialPipeline
{
    private readonly IFileSystem _fileSystem;

    private readonly IGameLanguageManager _languageManager;

    private readonly BuildSettings _settings;
    private readonly IPhysicalMod _mod;

    public BuildPipeline(IPhysicalMod mod, BuildSettings settings, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _mod = mod;
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _languageManager = serviceProvider
            .GetRequiredService<IGameLanguageManagerProvider>().GetLanguageManager(GameEngineType.Foc);
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        FailFast = true;
    }

    public override string ToString()
    {
        return $"Building {_mod.Name}";
    }

    protected override Task<IList<IStep>> CreateRunnerSteps(CancellationToken token)
    {
        return Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new RunPipelineStep(new PreBuildPipeline(this, ServiceProvider), ServiceProvider),
            new RunPipelineStep(new CoreBuildPipeline(this, ServiceProvider), ServiceProvider)
        });
    }

    private IEnumerable<IStep> CreateBuildSteps()
    {
        yield return new PackMegFileStep(new RawAiPackMegConfiguration(_mod, ServiceProvider), _settings, ServiceProvider);
        yield return new PackMegFileStep(new RawCustomMapsPackMegConfiguration(_mod, ServiceProvider), _settings, ServiceProvider);
        yield return new PackMegFileStep(new RawNonLocalizedSfxMegConfiguration(_mod, ServiceProvider), _settings, ServiceProvider);
        yield return new PackIconsStep(_settings, ServiceProvider);
        yield return new CompileLocalizationStep(_settings, ServiceProvider);

        foreach (var focLanguage in _languageManager.SupportedLanguages)
        {
            var isRaWSupported = IsSupportedByRaw(focLanguage);

            // There is no need to build non-supported languages if we don't do a release or force a clean build
            if (!isRaWSupported && !_settings.CleanBuild)
                continue;

            yield return new PackMegFileStep(
                new RawLocalizedSfx2DMegConfiguration(focLanguage, isRaWSupported, _mod, ServiceProvider),
                _settings,
                ServiceProvider);
        }
    }

    private IList<IStep> CreatePreBuildSteps()
    {
        return new List<IStep>
        {
            new CleanOutdatedAssetsStep(_mod, ServiceProvider)
        };
    }

    private bool IsSupportedByRaw(LanguageType focLanguage)
    {
        var path = _fileSystem.Path.Combine(_mod.Directory.FullName, "Data/Audio/Units", focLanguage.ToString());
        return _fileSystem.Directory.Exists(path);
    }

    private class CoreBuildPipeline(BuildPipeline parent, IServiceProvider serviceProvider)
        : StepRunnerPipeline(serviceProvider)
    {
        protected override Task<IList<IStep>> CreateRunnerSteps(CancellationToken token)
        {
            return Task.FromResult<IList<IStep>>(parent.CreateBuildSteps().ToList());
        }

        protected override IStepRunner CreateRunner()
        {
            return new AsyncStepRunner(4, ServiceProvider);
        }
    }
    
    private class PreBuildPipeline(BuildPipeline parent, IServiceProvider serviceProvider)
        : SequentialPipeline(serviceProvider)
    {
        protected override Task<IList<IStep>> CreateRunnerSteps(CancellationToken token)
        {
            return Task.FromResult(parent.CreatePreBuildSteps());
        }
    }
}