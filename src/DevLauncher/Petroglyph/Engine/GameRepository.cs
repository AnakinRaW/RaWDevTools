using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using AnakinRaW.CommonUtilities.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.Commons.Hashing;
using PG.StarWarsGame.Files.MEG.Data.Archives;
using PG.StarWarsGame.Files.MEG.Files;
using PG.StarWarsGame.Files.MEG.Services;
using PG.StarWarsGame.Files.MEG.Services.Builder.Normalization;
using PG.StarWarsGame.Infrastructure;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using PG.StarWarsGame.Infrastructure.Services.Dependencies;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;
using RepublicAtWar.DevLauncher.Petroglyph.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph.Engine;

public class GameRepository : IGameRepository
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileSystem _fileSystem;
    private readonly PetroglyphDataEntryPathNormalizer _megPathNormalizer;
    private readonly ICrc32HashingService _crc32HashingService;
    private readonly IMegFileExtractor _megExtractor;
    private readonly IMegFileService _megFileService;
    private readonly ILogger? _logger;

    private readonly string _gameDirectory;

    private readonly IList<string> _modPaths = new List<string>();

    private readonly string _fallbackPath;

    private readonly IVirtualMegArchive? _masterMegArchive;

    public IGameRepository EffectsRepository { get; }


    public GameRepository(IPhysicalPlayableObject playableObject, IGame fallbackGame, IServiceProvider serviceProvider)
        : this(playableObject.Game,
            fallbackGame, serviceProvider)
    {
        if (playableObject == null)
            throw new ArgumentNullException(nameof(playableObject));

        if (playableObject is IPhysicalMod mod)
        {
            if (mod.DependencyResolveStatus == DependencyResolveStatus.Resolved)
            {
                var mods = serviceProvider.GetRequiredService<IModDependencyTraverser>().Traverse(mod);
                foreach (var entry in mods)
                {
                    if (entry.Mod is IPhysicalMod physicalMod)
                        _modPaths.Add(physicalMod.Directory.FullName);
                }
            }
            else
                _modPaths.Add(playableObject.Directory.FullName);
        }
    }

    public GameRepository(IPhysicalMod[] mods, IGame baseGame, IGame fallbackGame, IServiceProvider serviceProvider) :
        this(baseGame, fallbackGame, serviceProvider)
    {
        if (mods == null)
            throw new ArgumentNullException(nameof(mods));

        foreach (var mod in mods)
            _modPaths.Add(mod.Directory.FullName);
    }

    public GameRepository(IGame baseGame, IGame fallbackGame, IServiceProvider serviceProvider)
    {
        if (fallbackGame == null)
            throw new ArgumentNullException(nameof(fallbackGame));
        if (fallbackGame == null)
            throw new ArgumentNullException(nameof(fallbackGame));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _megPathNormalizer = serviceProvider.GetRequiredService<PetroglyphDataEntryPathNormalizer>();
        _crc32HashingService = serviceProvider.GetRequiredService<ICrc32HashingService>();
        _megExtractor = serviceProvider.GetRequiredService<IMegFileExtractor>();
        _megFileService = serviceProvider.GetRequiredService<IMegFileService>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());

        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

        _gameDirectory = _fileSystem.Path.GetFullPath(baseGame.Directory.FullName);
        _fallbackPath = _fileSystem.Path.GetFullPath(fallbackGame.Directory.FullName);

        _masterMegArchive = CreateMasterMegArchive();

        EffectsRepository = new EffectsRepository(this, serviceProvider);
    }

    private IVirtualMegArchive CreateMasterMegArchive()
    {
        var builder = _serviceProvider.GetRequiredService<IVirtualMegArchiveBuilder>();

        var megsToConsider = new List<IMegFile>();

        var eawMegs = LoadMegArchivesFromXml(_fallbackPath);
        var eawPatch = LoadMegArchive(_fileSystem.Path.Combine(_fallbackPath, "Data\\Patch.meg"));
        var eawPatch2 = LoadMegArchive(_fileSystem.Path.Combine(_fallbackPath, "Data\\Patch2.meg"));
        var eaw64Patch = LoadMegArchive(_fileSystem.Path.Combine(_fallbackPath, "Data\\64Patch.meg"));

        var focOrModMegs = LoadMegArchivesFromXml(".");
        var focPatch = LoadMegArchive("Data\\Patch.meg");
        var focPatch2 = LoadMegArchive("Data\\Patch2.meg");
        var foc64Patch = LoadMegArchive("Data\\64Patch.meg");

        megsToConsider.AddRange(eawMegs);
        if (eawPatch is not null)
            megsToConsider.Add(eawPatch);
        if (eawPatch2 is not null)
            megsToConsider.Add(eawPatch2);
        if (eaw64Patch is not null)
            megsToConsider.Add(eaw64Patch);

        megsToConsider.AddRange(focOrModMegs);
        if (focPatch is not null)
            megsToConsider.Add(focPatch);
        if (focPatch2 is not null)
            megsToConsider.Add(focPatch2);
        if (foc64Patch is not null)
            megsToConsider.Add(foc64Patch);

        return builder.BuildFrom(megsToConsider, true);
    }

    private IList<IMegFile> LoadMegArchivesFromXml(string lookupPath)
    {
        var megFilesXmlPath = _fileSystem.Path.Combine(lookupPath, "Data\\MegaFiles.xml");

        using var xmlStream = TryOpenFile(megFilesXmlPath);

        if (xmlStream is null)
        {
            _logger?.LogWarning($"Unable to find MegaFiles.xml at '{lookupPath}'");
            return Array.Empty<IMegFile>();
        }

        var parser = PetroglyphXmlParserFactory.Instance.GetFileParser<XmlFileContainer>(_serviceProvider);
        var megaFilesXml = parser.ParseFile(xmlStream);



        var megs = new List<IMegFile>(megaFilesXml.Files.Count);

        foreach (var file in megaFilesXml.Files.Select(x => x.Trim()))
        {
            var megPath = _fileSystem.Path.Combine(lookupPath, file);
            var megFile = LoadMegArchive(megPath);
            if (megFile is not null)
                megs.Add(megFile);
        }

        return megs;
    }

    private IMegFile? LoadMegArchive(string megPath)
    {
        using var megFileStream = TryOpenFile(megPath);
        if (megFileStream is not FileSystemStream fileSystemStream)
        {
            _logger?.LogTrace($"Unable to find MEG data at '{megPath}'");
            return null;
        }

        var megFile = _megFileService.Load(fileSystemStream);

        if (megFile.FileInformation.FileVersion != MegFileVersion.V1)
            throw new InvalidOperationException("MEG data version must be V1.");

        return megFile;
    }

    public Stream OpenFile(string filePath, bool megFileOnly = false)
    {
        var stream = TryOpenFile(filePath, megFileOnly);
        if (stream is null)
            throw new FileNotFoundException($"Unable to find game data: {filePath}");
        return stream;
    }

    public bool FileExists(string filePath, string[] extensions, bool megFileOnly = false)
    {
        foreach (var extension in extensions)
        {
            var newPath = _fileSystem.Path.ChangeExtension(filePath, extension);
            if (FileExists(newPath, megFileOnly))
                return true;
        }
        return false;
    }


    public bool FileExists(string filePath, bool megFileOnly = false)
    {
        if (!megFileOnly)
        {
            // This is a custom rule
            if (_fileSystem.Path.IsPathFullyQualified(filePath))
                return _fileSystem.File.Exists(filePath);

            foreach (var modPath in _modPaths)
            {
                var modFilePath = _fileSystem.Path.Combine(modPath, filePath);
                if (_fileSystem.File.Exists(modFilePath))
                    return true;
            }

            var normalFilePath = _fileSystem.Path.Combine(_gameDirectory, filePath);
            if (_fileSystem.File.Exists(normalFilePath))
                return true;
        }

        if (_masterMegArchive is not null)
        {
            var normalizedPath = _megPathNormalizer.Normalize(filePath);
            var crc = _crc32HashingService.GetCrc32(normalizedPath, PGConstants.PGCrc32Encoding);

            var entry = _masterMegArchive.FirstEntryWithCrc(crc);
            if (entry is not null)
                return true;
        }

        if (!megFileOnly)
        {
            var fallbackPath = _fileSystem.Path.Combine(_fallbackPath, filePath);
            if (_fileSystem.File.Exists(fallbackPath))
                return true;
        }

        return false;
    }

    public Stream? TryOpenFile(string filePath, bool megFileOnly = false)
    {
        if (!megFileOnly)
        {
            // This is a custom rule
            if (_fileSystem.Path.IsPathFullyQualified(filePath))
                return !_fileSystem.File.Exists(filePath) ? null : OpenFileRead(filePath);

            foreach (var modPath in _modPaths)
            {
                var modFilePath = _fileSystem.Path.Combine(modPath, filePath);
                if (_fileSystem.File.Exists(modFilePath))
                    return OpenFileRead(modFilePath);
            }


            var normalFilePath = _fileSystem.Path.Combine(_gameDirectory, filePath);
            if (_fileSystem.File.Exists(normalFilePath))
                return OpenFileRead(normalFilePath);
        }

        if (_masterMegArchive is not null)
        {
            var normalizedPath = _megPathNormalizer.Normalize(filePath);
            var crc = _crc32HashingService.GetCrc32(normalizedPath, PGConstants.PGCrc32Encoding);

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
        if (!AllowOpenFile(filePath))
            throw new UnauthorizedAccessException("The data is not part of the Games!");
        return _fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    private bool AllowOpenFile(string filePath)
    {
        foreach (var modPath in _modPaths)
        {
            if (_fileSystem.Path.IsChildOf(modPath, filePath))
                return true;
        }

        if (_fileSystem.Path.IsChildOf(_gameDirectory, filePath))
            return true;
        if (_fileSystem.Path.IsChildOf(_fallbackPath, filePath))
            return true;

        return false;
    }
}