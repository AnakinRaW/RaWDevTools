using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.DevTools.Steps.Releasing;

public class VerifyLocalizationStep(
    IPhysicalMod republicAtWar,
    BuildSettings buildSettings,
    bool modVersionIsPrerelease,
    IServiceProvider serviceProvider) : PipelineStep(serviceProvider)
{
    private readonly ILogger? _logger =
        serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(VerifyLocalizationStep));


    protected override Task RunCoreAsync(CancellationToken token)
    {
        return Task.Run(() => RunCore(token), CancellationToken.None);
    }

    private void RunCore(CancellationToken token)
    { 
        _logger?.LogInformation("Verifying localization...");
    }
}