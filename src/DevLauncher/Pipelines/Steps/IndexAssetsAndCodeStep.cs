using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Petroglyph;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps;

internal class IndexAssetsAndCodeStep : SynchronizedStep
{
    private readonly IPhysicalMod _mod;
    private readonly IGame _fallbackGame;
    private readonly DevToolsOptionBase _options;
    private readonly ILogger? _logger;

    internal GameDatabase GameDatabase { get; private set; } = null!;

    public IndexAssetsAndCodeStep(IPhysicalMod mod, IGame fallbackGame, DevToolsOptionBase options, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = Services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _mod = mod ?? throw new ArgumentNullException(nameof(mod));
        _fallbackGame = fallbackGame;
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    protected override void RunSynchronized(CancellationToken token)
    {
        _logger?.LogInformation("Indexing Republic at War...");

        var gameRepository = new GameRepository(_mod, _fallbackGame, Services);


        // GUIDialogs.xml
        // LensFlares.xml
        // SurfaceFX.xml
        // TerrainDecalFX.xml
        // GraphicDetails.xml
        // DynamicTrackFX.xml
        // ShadowBlobMaterials.xml
        // TacticalCameras.xml
        // LightSources.xml
        // StarWars3DTextCrawl.xml
        // MusicEvents.xml
        // SpeechEvents.xml
        // GameConstants.xml
        // Audio.xml
        // WeatherAudio.xml
        // HeroClash.xml
        // TradeRouteLines.xml
        // RadarMap.xml
        // WeatherModifiers.xml
        // Movies.xml
        // LightningEffectTypes.xml
        // DifficultyAdjustments.xml
        // WeatherScenarios.xml
        // UnitAbilityTypes.xml
        // BlackMarketItems.xml
        // MovementClassTypeDefs.xml
        // AITerrainEffectiveness.xml


        // CONTAINER FILES:
        // GameObjectFiles.xml
        // SFXEventFiles.xml
        // CommandBarComponentFiles.xml
        // TradeRouteFiles.xml
        // HardPointDataFiles.xml
        // CampaignFiles.xml
        // FactionFiles.xml
        // TargetingPrioritySetFiles.xml
        // MousePointerFiles.xml

        var indexGamesPipeline = new InitializeGameDatabasePipeline(gameRepository, Services);
        indexGamesPipeline.RunAsync(token).Wait();

        GameDatabase = indexGamesPipeline.GameDatabase;

        _logger?.LogInformation("Finished indexing");
    }
}