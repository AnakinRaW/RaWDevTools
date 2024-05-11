using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using AnakinRaW.CommonUtilities.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.Commons.Hashing;
using PG.StarWarsGame.Files.MEG.Data.Archives;
using PG.StarWarsGame.Files.MEG.Files;
using PG.StarWarsGame.Files.MEG.Services;
using PG.StarWarsGame.Files.MEG.Services.Builder.Normalization;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;
using RepublicAtWar.DevLauncher.Petroglyph.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph;

public class GameRepository
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileSystem _fileSystem;
    private readonly PetroglyphDataEntryPathNormalizer _megPathNormalizer;
    private readonly ICrc32HashingService _crc32HashingService;
    private readonly IMegFileExtractor _megExtractor;
    private readonly IMegFileService _megFileService;
    private readonly ILogger? _logger;

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
        _megFileService = serviceProvider.GetRequiredService<IMegFileService>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());

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
            _logger?.LogTrace($"Unable to find MEG file at '{megPath}'");
            return null;
        }

        var megFile = _megFileService.Load(fileSystemStream);

        if (megFile.FileInformation.FileVersion != MegFileVersion.V1)
            throw new InvalidOperationException("MEG file version must be V1.");

        return megFile;
    }

    public Stream OpenFile(string filePath, bool megFileOnly = false)
    {
        var stream = TryOpenFile(filePath, megFileOnly);
        if (stream is null)
            throw new FileNotFoundException($"Unable to find game file: {filePath}");
        return stream;
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

        _logger?.LogTrace($"Unable to find file '{filePath}'");

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