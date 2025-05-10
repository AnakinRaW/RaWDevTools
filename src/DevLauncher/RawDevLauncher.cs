using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Update.Options;
using AnakinRaW.CommonUtilities.SimplePipeline;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines;
using RepublicAtWar.DevLauncher.Pipelines.Actions;
using RepublicAtWar.DevLauncher.Pipelines.Settings;
using RepublicAtWar.DevLauncher.Update;
using RepublicAtWar.DevTools.Services;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.DevLauncher;

internal sealed class RawDevLauncher(UpdatableApplicationEnvironment applicationEnvironment, IServiceProvider serviceProvider)
{
    private readonly CancellationTokenSource _applicationCancellationTokenSource = new();
    private readonly Parser _looseArgumentParser = new(c => { c.IgnoreUnknownArguments = true; });
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(RawDevLauncher));

    public async Task<int> RunAsync(IReadOnlyList<string> args)
    {
        _logger?.LogDebug("Command line arguments: " + string.Join(" ", args));

        var skipUpdate = false;
        _looseArgumentParser.ParseArguments<SkipUpdateOption>(args).WithParsed(su => skipUpdate = su.SkipUpdate);

        if (!skipUpdate)
        {
            _logger?.LogDebug("Running update routine.");
            ApplicationUpdateOptions? options = null;
            _looseArgumentParser.ParseArguments<ApplicationUpdateOptions>(args).WithParsed(updateOptions => options = updateOptions);

            var updater = new RawDevLauncherUpdater(applicationEnvironment, serviceProvider);
            var branch = updater.CreateBranch(options?.BranchName, options?.ManifestUrl);
            await updater.AutoUpdateApplication(branch);
        }
        else
        {
            _logger?.LogDebug("Skipping update routine.");
        }

        Type[] optionTypes =
        [
            typeof(BuildAndRunOption),
                typeof(InitializeLocalizationOption),
                typeof(PrepareLocalizationsOption),
                typeof(MergeLocalizationOption),
                typeof(ReleaseRepublicAtWarOption),
                typeof(VerifyOption)
        ];

        var toolResult = 0;
        var parseResult = _looseArgumentParser.ParseArguments(args, optionTypes);

        await parseResult.WithParsedAsync(async o =>
        {
            toolResult = await RunCore((DevToolsOptionBase)o);
        });

        await parseResult.WithNotParsedAsync(_ =>
        {
            Console.WriteLine(HelpText.AutoBuild(parseResult).ToString());
            toolResult = 0xA0;
            return Task.CompletedTask;
        });

        return toolResult;
    }

    private async Task<int> RunCore(DevToolsOptionBase options)
    {
        try
        {
            var gameFinderResult = new ModFinderService(serviceProvider).FindAndAddModInCurrentDirectory();

            IPipeline launcherPipeline;

            switch (options)
            {
                case BuildAndRunOption buildRun:
                    launcherPipeline = CreateBuildRunPipeline(buildRun, gameFinderResult, serviceProvider);
                    break;
                case InitializeLocalizationOption:
                    launcherPipeline = new InitializeLocalizationAction(serviceProvider);
                    break;
                case PrepareLocalizationsOption:
                    launcherPipeline = new CreateLocalizationDiffsAction(serviceProvider);
                    break;
                case ReleaseRepublicAtWarOption rawRelease:
                    launcherPipeline = CreateReleasePipeline(rawRelease, gameFinderResult, serviceProvider);
                    break;
                case MergeLocalizationOption:
                    launcherPipeline = new MergeLocalizationsAction(serviceProvider);
                    break;
                case VerifyOption verifyOption:
                    launcherPipeline = CreateBuildVerifyPipeline(verifyOption, gameFinderResult, serviceProvider);
                    break;
                default:
                    throw new ArgumentException($"The option '{options.GetType().FullName}' is not implemented",
                        nameof(options));
            }

            await launcherPipeline.RunAsync(_applicationCancellationTokenSource.Token);

            _logger?.LogInformation("DONE");

            return 0;
        }
        catch (Exception e)
        {
            _logger?.LogError(e.Message, e);
            return e.HResult;
        }
    }

    private static IPipeline CreateBuildVerifyPipeline(VerifyOption options, GameFinderResult gameFinderResult, IServiceProvider services)
    {
        var buildSettings = new BuildSettings
        {
            WarnAsError = options.WarnAsError,
            CleanBuild = options.CleanBuild
        };
        return new BuildAndVerifyPipeline(gameFinderResult.RepublicAtWar, gameFinderResult.FallbackGame, buildSettings, services);
    }

    private static IPipeline CreateBuildRunPipeline(BuildAndRunOption options, GameFinderResult gameFinderResult, IServiceProvider services)
    {
        var buildSettings = new BuildSettings
        {
            CleanBuild = options.CleanBuild,
            WarnAsError = options.WarnAsError
        };
        var launchSettings = new LaunchSettings
        {
            RunGame = !options.SkipRun,
            Debug = options.Debug,
            Windowed = options.Windowed
        };
        return new BuildAndRunPipeline(gameFinderResult.RepublicAtWar, buildSettings, launchSettings, services);
    }

    private static IPipeline CreateReleasePipeline(ReleaseRepublicAtWarOption options, GameFinderResult gameFinderResult, IServiceProvider services)
    {
        var buildSettings = new BuildSettings
        {
            CleanBuild = options.CleanBuild,
            WarnAsError = options.WarnAsError
        };
        var releaseSettings = new ReleaseSettings
        {
            UploaderDirectory = options.UploaderDirectory,
            WarnAsError = options.WarnAsError
        };
        return new ReleaseRawPipeline(gameFinderResult.RepublicAtWar, gameFinderResult.FallbackGame, buildSettings, releaseSettings, services);
    }
}