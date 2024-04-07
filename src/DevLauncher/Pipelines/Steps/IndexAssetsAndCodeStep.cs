using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Database;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Services;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps;

internal class IndexAssetsAndCodeStep : SynchronizedStep
{
    private readonly IPhysicalMod _mod;
    private readonly DevToolsOptionBase _options;
    private readonly ILogger? _logger;

    internal ModDatabase ModDatabase { get; private set; }

    public IndexAssetsAndCodeStep(IPhysicalMod mod, DevToolsOptionBase options, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = Services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _mod = mod ?? throw new ArgumentNullException(nameof(mod));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    protected override void SynchronizedInvoke(CancellationToken token)
    {
        _logger?.LogInformation("Indexing Republic at War...");

        var englishLocalization = new LocalizationFileService(_options, Services).LoadLocalization("MasterTextFile_English.txt");

        var database = new ModDatabase(englishLocalization);

        ModDatabase = database;

        _logger?.LogInformation("Finished indexing");
    }
}