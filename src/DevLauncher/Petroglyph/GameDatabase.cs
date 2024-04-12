using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using AnakinRaW.CommonUtilities.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using PG.Commons.Hashing;
using PG.StarWarsGame.Files.MEG.Data.Archives;
using PG.StarWarsGame.Files.MEG.Files;
using PG.StarWarsGame.Files.MEG.Services;
using PG.StarWarsGame.Files.MEG.Services.Builder.Normalization;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;

namespace RepublicAtWar.DevLauncher.Petroglyph;

public class GameDatabase
{
    public GameRepository GameRepository { get; }

    public GameDatabase(GameRepository gameRepository)
    {
        GameRepository = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));
    }
}

public class GameRepository
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileSystem _fileSystem;
    private readonly PetroglyphDataEntryPathNormalizer _megPathNormalizer;
    private readonly ICrc32HashingService _crc32HashingService;
    private readonly IMegFileExtractor _megExtractor;

    private readonly string _gameDirectory;

    // TODO: In a full feature implementation these must be lists
    private readonly string _modPath;
    private readonly string _fallbackPath;

    private readonly IVirtualMegArchive? _masterMegArchive;

    public GameRepository(IPhysicalMod mod, IGame fallbackGame, IServiceProvider serviceProvider)
    {
        if (mod == null) 
            throw new ArgumentNullException(nameof(mod));
        if (fallbackGame == null) 
            throw new ArgumentNullException(nameof(fallbackGame));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _megPathNormalizer = serviceProvider.GetRequiredService<PetroglyphDataEntryPathNormalizer>();
        _crc32HashingService = serviceProvider.GetRequiredService<ICrc32HashingService>();
        _megExtractor = serviceProvider.GetRequiredService<IMegFileExtractor>();

        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

        _modPath = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(mod.Directory.FullName));
        _gameDirectory = _fileSystem.Path.GetFullPath(mod.Game.Directory.FullName);
        _fallbackPath = _fileSystem.Path.GetFullPath(fallbackGame.Directory.FullName);

        _masterMegArchive = CreateMasterMegArchive();
    }

    private IVirtualMegArchive CreateMasterMegArchive()
    {
        var builder = _serviceProvider.GetRequiredService<IVirtualMegArchiveBuilder>();

        var megsToConsider = new List<IMegFile>();
        
        var eawMegs = LoadMegArchivesFromXml(_fileSystem.Path.Combine(_fallbackPath, "Data\\MegaFiles.xml"));
        var eawPath = LoadMegArchive(_fileSystem.Path.Combine(_fallbackPath, "Data\\Patch.meg"));
        var eawPath2 = LoadMegArchive(_fileSystem.Path.Combine(_fallbackPath, "Data\\Patch2.meg"));
        var eaw64Path = LoadMegArchive(_fileSystem.Path.Combine(_fallbackPath, "Data\\64Patch.meg"));

        var focOrModMegs = LoadMegArchivesFromXml("Data\\MegaFiles.xml");
        var focPath = LoadMegArchive("Data\\Patch.meg");
        var focPath2 = LoadMegArchive("Data\\Patch2.meg");
        var foc64Path = LoadMegArchive("Data\\64Patch.meg");

        megsToConsider.AddRange(eawMegs);
        megsToConsider.Add(eawPath);
        megsToConsider.Add(eawPath2);
        megsToConsider.Add(eaw64Path);

        megsToConsider.AddRange(focOrModMegs);
        megsToConsider.Add(focPath);
        megsToConsider.Add(focPath2);
        megsToConsider.Add(foc64Path);

        return builder.BuildFrom(megsToConsider, true);
    }

    private IList<IMegFile> LoadMegArchivesFromXml(string megFilesXmlPath)
    {
        using var xmlStream = TryOpenFile(megFilesXmlPath);

        return new List<IMegFile>();
    }

    private IMegFile LoadMegArchive(string megPath)
    {
        return null;
    }


    public Stream? TryOpenFile(string filePath, bool megFileOnly = false)
    {
        if (!megFileOnly)
        {
            // This is a custom rule
            if (_fileSystem.Path.IsPathFullyQualified(filePath))
                return !_fileSystem.File.Exists(filePath) ? null : OpenFileRead(filePath);

            var modFilePath = _fileSystem.Path.Combine(_modPath, filePath);
            if (_fileSystem.File.Exists(modFilePath))
                return OpenFileRead(modFilePath);

            var normalFilePath = _fileSystem.Path.Combine(_gameDirectory, filePath);
            if (_fileSystem.File.Exists(normalFilePath))
                return OpenFileRead(normalFilePath);
        }

        if (_masterMegArchive is not null)
        {
            var normalizedPath = _megPathNormalizer.Normalize(filePath);
            var crc = _crc32HashingService.GetCrc32(normalizedPath, Encoding.ASCII);

            var entry = _masterMegArchive.FirstEntryWithCrc(crc);
            if (entry is not null)
                return _megExtractor.GetFileData(entry.Location);
        }

        if (!megFileOnly)
        {
            var fallbackPath = _fileSystem.Path.Combine(_fallbackPath, filePath);
            if (_fileSystem.File.Exists(fallbackPath))
                return OpenFileRead(fallbackPath);
        }

        return null;
    }

    private FileSystemStream OpenFileRead(string filePath)
    {
        if (!_fileSystem.Path.IsChildOf(_modPath, filePath) &&
            !_fileSystem.Path.IsChildOf(_gameDirectory, filePath) &&
            !_fileSystem.Path.IsChildOf(_fallbackPath, filePath))
            throw new UnauthorizedAccessException("The file is not part of the Games!");

        return _fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }
}

public sealed class MegaFilesXml
{
    
}