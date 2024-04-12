using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Petroglyph;
using RepublicAtWar.DevLauncher.Services;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps;

internal class IndexAssetsAndCodeStep : SynchronizedStep
{
    private readonly IPhysicalMod _mod;
    private readonly IGame _fallbackGame;
    private readonly DevToolsOptionBase _options;
    private readonly ILogger? _logger;

    internal GameDatabase GameDatabase { get; private set; }

    public IndexAssetsAndCodeStep(IPhysicalMod mod, IGame fallbackGame, DevToolsOptionBase options, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = Services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _mod = mod ?? throw new ArgumentNullException(nameof(mod));
        _fallbackGame = fallbackGame;
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    protected override void SynchronizedInvoke(CancellationToken token)
    {
        _logger?.LogInformation("Indexing Republic at War...");

        var englishLocalization = new LocalizationFileService(_options, Services).LoadLocalization("MasterTextFile_English.txt");

        var gameRepository = new GameRepository(_mod, _fallbackGame, Services);


        _logger?.LogInformation("Finished indexing");
    }
}