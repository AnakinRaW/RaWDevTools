using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Files.MEG.Files;
using PG.StarWarsGame.Files.MEG.Services.Builder;
using RepublicAtWar.DevLauncher.Configuration;
using DirectoryInfoWrapper = Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper;

namespace RepublicAtWar.DevLauncher.Services;

internal class MegPackerService(IServiceProvider serviceProvider) : IMegPackerService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(MegPackerService));

    public void Pack(IPackMegConfiguration configuration)
    {
        var megBuilder = new EmpireAtWarMegBuilder(configuration.VirtualRootDirectory.FullName, _serviceProvider);

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        foreach (var fileToPack in configuration.FilesToPack) 
            matcher.AddInclude(fileToPack);

        var matcherResult = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(configuration.VirtualRootDirectory.FullName)));

        var files = matcherResult.Files.Select(f => f.Path).ToList();
        var megFilePath = _fileSystem.Path.Combine(configuration.VirtualRootDirectory.FullName, configuration.FileName);

        var updateChecker = _serviceProvider.GetRequiredService<IBinaryRequiresUpdateChecker>();
        if (!updateChecker.RequiresUpdate(megFilePath, files))
        {
            _logger?.LogDebug($"MEG file '{_fileSystem.Path.GetFileName(megFilePath)}' is already up to date. Skipping build.");
            return;
        }

        foreach (var file in files)
        {
            var filePath = file;
            if (configuration.FileNamesOnly)
                filePath = _fileSystem.Path.GetFileName(filePath);

            var entryPath = megBuilder.ResolveEntryPath(filePath);
            if (entryPath is null)
                throw new InvalidOperationException($"Entry path for '{file}' could not be resolved.");
            
            var result = megBuilder.AddFile(_fileSystem.Path.Combine(configuration.VirtualRootDirectory.FullName, file), entryPath);
            if (!result.Added)
                throw new InvalidOperationException(result.Message);
        }

        megBuilder.Build(new MegFileInformation(megFilePath, MegFileVersion.V1), true);
    }

    public void Dispose()
    {
    }
}