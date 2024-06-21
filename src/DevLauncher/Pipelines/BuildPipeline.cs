using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Engine.Language;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevTools.Steps.Build;
using RepublicAtWar.DevTools.Steps.Build.Meg;
using RepublicAtWar.DevTools.Steps.Build.Meg.Config;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal sealed class BuildPipeline(IPhysicalMod mod, BuildSettings settings, IServiceProvider serviceProvider)
    : Pipeline(serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly IGameLanguageManager _languageManager = serviceProvider.GetRequiredService<IGameLanguageManager>();
    private readonly BuildSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    private readonly List<IStep> _buildSteps = new();
    private readonly List<IStep> _preBuildSteps = new();

    private readonly ParallelRunner _buildRunner = new(4, serviceProvider);
    private readonly StepRunner _preBuildRunner = new(serviceProvider);

    protected override bool FailFast => true;

    public override string ToString()
    {
        return $"Building {mod.Name}";
    }

    protected override Task<bool> PrepareCoreAsync()
    {
        _preBuildSteps.Clear();
        _preBuildSteps.AddRange(CreatePreBuildSteps());
        foreach (var buildStep in _preBuildSteps)
            _preBuildRunner.AddStep(buildStep);

        _buildSteps.Clear();
        _buildSteps.AddRange(CreateBuildSteps());
        foreach (var buildStep in _buildSteps) 
            _buildRunner.AddStep(buildStep);

        return Task.FromResult(true);
    }

    private IEnumerable<IStep> CreateBuildSteps()
    {
        yield return new PackMegFileStep(new RawAiPackMegConfiguration(mod, ServiceProvider), _settings, ServiceProvider);
        yield return new PackMegFileStep(new RawCustomMapsPackMegConfiguration(mod, ServiceProvider), _settings, ServiceProvider);
        yield return new PackMegFileStep(new RawNonLocalizedSFXMegConfiguration(mod, ServiceProvider), _settings, ServiceProvider);
        yield return new PackIconsStep(_settings, ServiceProvider);
        yield return new CompileLocalizationStep(_settings, ServiceProvider);

        foreach (var focLanguage in _languageManager.FocSupportedLanguages)
        {
            var isRaWSupported = IsSupportedByRaw(focLanguage);

            // There is no need to build non-supported languages if we don't do a release or force a clean build
            if (!isRaWSupported && !_settings.CleanBuild)
                continue;

            yield return new PackMegFileStep(
                new RawLocalizedSFX2DMegConfiguration(focLanguage, isRaWSupported, mod, ServiceProvider),
                _settings,
                ServiceProvider);
        }
    }

    private IList<IStep> CreatePreBuildSteps()
    {
        return new List<IStep>
        {
            new CleanOutdatedAssetsStep(mod, ServiceProvider)
        };
    }

    protected override async Task RunCoreAsync(CancellationToken token)
    {
        try
        {
            Logger?.LogInformation("Running Prebuild...");
            _preBuildRunner.Error -= OnError;
            await _preBuildRunner.RunAsync(token);
        }
        finally
        {
            Logger?.LogInformation("Finished Prebuild...");
            _preBuildRunner.Error -= OnError;
        }

        ThrowIfAnyStepsFailed(_preBuildSteps);

        try
        {
            Logger?.LogInformation("Running Build...");
            _buildRunner.Error -= OnError;
            await _buildRunner.RunAsync(token);
        }
        finally
        {
            Logger?.LogInformation("Finished Build...");
            _buildRunner.Error -= OnError;
        }

        ThrowIfAnyStepsFailed(_buildSteps);
    }

    private bool IsSupportedByRaw(LanguageType focLanguage)
    {
        var path = _fileSystem.Path.Combine(mod.Directory.FullName, "Data/Audio/Units", focLanguage.ToString());
        return _fileSystem.Directory.Exists(path);
    }
}