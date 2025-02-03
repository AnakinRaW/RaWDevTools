using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Files.MEG.Files;
using PG.StarWarsGame.Files.MEG.Services.Builder;
using RepublicAtWar.DevTools.Services;
using RepublicAtWar.DevTools.Steps.Build.Meg.Config;
using RepublicAtWar.DevTools.Steps.Settings;
using DirectoryInfoWrapper = Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper;

namespace RepublicAtWar.DevTools.Steps.Build.Meg;

public class PackMegFileStep(IPackMegConfiguration config, BuildSettings settings, IServiceProvider serviceProvider)
    : PipelineStep(serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(PackMegFileStep));

    private readonly IPackMegConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));

    protected override void RunCore(CancellationToken token)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        foreach (var fileToPack in _config.FilesToPack)
            matcher.AddInclude(fileToPack);

        var matcherResult = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(_config.VirtualRootDirectory.FullName)));

        var files = matcherResult.Files.Select(f => f.Path).ToList();
        var megFilePath = _fileSystem.Path.Combine(_config.VirtualRootDirectory.FullName, _config.FileName);

        var megFileName = _fileSystem.Path.GetFileName(megFilePath);

        var updateChecker = _serviceProvider.GetRequiredService<IBinaryRequiresUpdateChecker>();

        if (!settings.CleanBuild && !updateChecker.RequiresUpdate(megFilePath, files))
        {
            _logger?.LogDebug($"MEG data '{megFileName}' is already up to date. Skipping build.");
            return;
        }

        _logger?.LogInformation($"Writing MEG data '{megFileName}'...");

        using var megBuilder = new EmpireAtWarMegBuilder(_config.VirtualRootDirectory.FullName, _serviceProvider);

        foreach (var file in files)
        {
            var filePath = file;
            if (_config.FileNamesOnly)
                filePath = _fileSystem.Path.GetFileName(filePath);

            if (_config.ModifyFileNameAction is not null)
                filePath = _config.ModifyFileNameAction(filePath);

            var entryPath = megBuilder.ResolveEntryPath(filePath);
            if (entryPath is null)
                throw new InvalidOperationException($"Entry path for '{file}' could not be resolved.");

            var result = megBuilder.AddFile(_fileSystem.Path.Combine(_config.VirtualRootDirectory.FullName, file),
                entryPath);
            if (!result.Added)
                throw new InvalidOperationException(result.Message);
        }

        megBuilder.Build(new MegFileInformation(megFilePath, MegFileVersion.V1), true);
        _logger?.LogInformation($"Finished writing MEG data '{megFileName}'...");
    }
}