using System;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using PG.StarWarsGame.Files.MEG.Files;
using PG.StarWarsGame.Files.MEG.Services;
using PG.StarWarsGame.Files.MEG.Services.Builder;
using RepublicAtWar.DevLauncher.Configuration;
using Validation;
using Vanara.PInvoke;
using DirectoryInfoWrapper = Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper;

namespace RepublicAtWar.DevLauncher.Services;

internal class MegPackerService : IMegPackerService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileSystem _fileSystem;

    public MegPackerService(IServiceProvider serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _serviceProvider = serviceProvider;
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    }

    public void Pack(IPackMegConfiguration configuration)
    {
        var megBuilder = new EmpireAtWarMegBuilder(configuration.VirtualRootDirectory.FullName, _serviceProvider);

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        foreach (var fileToPack in configuration.FilesToPack)
        {
            matcher.AddInclude(fileToPack);
        }

        var matcherResult = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(configuration.VirtualRootDirectory.FullName)));

        foreach (var file in matcherResult.Files)
        {
            var filePath = file.Path;
            if (configuration.FileNamesOnly)
                filePath = _fileSystem.Path.GetFileName(filePath);

            var entryPath = megBuilder.ResolveEntryPath(filePath);
            if (entryPath is null)
                throw new InvalidOperationException($"Entry path for '{file.Path}' could not be resolved.");
            
            var result = megBuilder.AddFile(_fileSystem.Path.Combine(configuration.VirtualRootDirectory.FullName, file.Path), entryPath);
            if (!result.Added)
                throw new InvalidOperationException(result.Message);
        }

        var megFilePath = _fileSystem.Path.Combine(configuration.VirtualRootDirectory.FullName, configuration.FileName);

        megBuilder.Build(new MegFileInformation(megFilePath, MegFileVersion.V1), true);
    }

    public void Dispose()
    {
    }
}