using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Utilities;
using RepublicAtWar.DevTools.Steps.Releasing;
using RepublicAtWar.DevTools.Steps.Settings;
using Semver;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class ReleaseRawPipeline : SequentialPipeline
{
    private readonly ILogger? _logger;
    private readonly BuildSettings _buildSettings;
    private readonly ReleaseSettings _releaseSettings;
    private readonly IPhysicalMod _republicAtWar;
    private readonly IGame _empireAtWarGame;
    private readonly IFileSystem _fileSystem;

    private ProgressBarReporter? _progressBarReporter;

    private SemVersion _modVersion = null!;

    public ReleaseRawPipeline(
        IPhysicalMod republicAtWar, 
        IGame empireAtWarGame,
        BuildSettings buildSettings,
        ReleaseSettings releaseSettings,
        IServiceProvider serviceProvider) 
        : base(serviceProvider)
    {
        _buildSettings = buildSettings ?? throw new ArgumentNullException(nameof(buildSettings));
        _releaseSettings = releaseSettings ?? throw new ArgumentNullException(nameof(releaseSettings));
        _republicAtWar = republicAtWar ?? throw new ArgumentNullException(nameof(republicAtWar));
        _empireAtWarGame = empireAtWarGame;
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

        FailFast = true;
    }

    protected override Task<IList<IStep>> CreateRunnerSteps(CancellationToken token)
    {
        _modVersion = SemVersion.Parse(_fileSystem.File.ReadAllText("version.txt"), SemVersionStyles.Strict);

        return Task.Run<IList<IStep>>(() =>
        {
            var createArtifactStep = new CreateUploadMetaArtifactsStep(_modVersion, ServiceProvider);

            var copyStep = new CopyReleaseStep(createArtifactStep, _releaseSettings, ServiceProvider);
            _progressBarReporter = new(copyStep);

            return new List<IStep>
            {
                // Build
                new RunPipelineStep(new BuildPipeline(_republicAtWar, _buildSettings, ServiceProvider), ServiceProvider),
                // Verify
                // new RunPipelineStep(new VerifyPipeline(_options, _republicAtWar, _empireAtWarGame, ServiceProvider), ServiceProvider),
               
                new VerifyLocalizationStep(_republicAtWar, _modVersion.IsPrerelease, ServiceProvider),
                
                // Build Release artifacts
                createArtifactStep,
                // Copy to Release
                copyStep
            };
        }, CancellationToken.None);
    }

    protected override void OnExecuteStarted()
    {
        base.OnExecuteStarted();

        _logger?.LogInformation("Releasing {Raw} v{Version}...", "Republic at War", _modVersion);

        if (!_buildSettings.CleanBuild)
        {
            _logger?.LogWarning("Releasing without Clean build!!!");
            _logger?.LogWarning("Releasing without Clean build!!!");
            _logger?.LogWarning("Releasing without Clean build!!!");
        }

        if (_modVersion.IsPrerelease)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Building a preview version!!!");
            Console.WriteLine("Building a preview version!!!");
            Console.WriteLine("Building a preview version!!!");
            Console.ResetColor();
        }
    }

    protected override void DisposeResources()
    {
        _progressBarReporter?.Dispose();
        _progressBarReporter = null;
        base.DisposeResources();
    }
}