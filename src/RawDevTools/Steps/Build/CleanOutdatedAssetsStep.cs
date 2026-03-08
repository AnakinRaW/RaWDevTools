using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using AET.Modinfo.Spec;
using PG.StarWarsGame.Engine.Localization;
using RepublicAtWar.DevTools.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Mods;

namespace RepublicAtWar.DevTools.Steps.Build;

public class CleanOutdatedAssetsStep(IPhysicalMod mod, IServiceProvider serviceProvider)
    : PipelineStep(serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    protected override Task RunCoreAsync(CancellationToken token)
    {
        return Task.Run(() =>
        {
            Logger?.LogInformation("Cleaning outdated assets...");
            var matcher = new Matcher();
            matcher.AddInclude("Data/Audio/SFX/sfx2d_*.meg");

            foreach (var fileToDelete in matcher.GetResultsInFullPath(mod.Directory.FullName))
            {
                Logger?.LogDebug("Deleting old asset '{File}'", fileToDelete);
                _fileSystem.File.DeleteWithRetry(fileToDelete);
            }

            var republicAtWarService = new RepublicAtWarService(Services);
            foreach (var (language, support) in republicAtWarService.GetSupportedLanguages())
            {
                if (language == LanguageType.English)
                    continue;

                if (support.HasFlag(LanguageSupportLevel.Speech))
                    continue;

                var targetDir = _fileSystem.Path.Combine(mod.Directory.FullName, "Data\\Audio\\Speech", language.ToString());
                if (_fileSystem.Directory.Exists(targetDir))
                {
                    Logger?.LogDebug("Deleting (unsupported) localized speech directory '{Directory}'", targetDir);
                    _fileSystem.Directory.Delete(targetDir, true);
                }
            }

            Logger?.LogInformation("Finished cleaning outdated assets.");
        }, CancellationToken.None);
    }
}