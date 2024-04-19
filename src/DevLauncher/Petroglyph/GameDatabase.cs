using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.Commons.Hashing;
using PG.StarWarsGame.Files.MEG.Data.Archives;
using PG.StarWarsGame.Files.MEG.Files;
using PG.StarWarsGame.Files.MEG.Services;
using PG.StarWarsGame.Files.MEG.Services.Builder.Normalization;
using PG.StarWarsGame.Infrastructure.Games;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Pipelines.Steps;
using KeyNotFoundException = System.Collections.Generic.KeyNotFoundException;

namespace RepublicAtWar.DevLauncher.Petroglyph;



internal class InitializeGameDatabasePipeline(IServiceProvider serviceProvider, int workerCount = 4) : ParallelPipeline(serviceProvider, workerCount)
{
    private IStep _parseGameObjectsStep;

    protected override IList<IStep> BuildStepsOrdered()
    {
        throw new NotImplementedException();
    }


    private async Task Add(IList<IStep> queue)
    {
        await foreach (var t in CreateSteps())
        {
            queue.Add(t);
        }
    }


    public static async IAsyncEnumerable<IStep> CreateSteps()
    {
        yield return new CompileLocalizationStep(null!);
    }
}



public class GameDatabase(GameRepository gameRepository, IServiceProvider serviceProvider)
{
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(GameDatabase));

    public GameRepository GameRepository { get; } = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));

    public Task Initialize(CancellationToken token = default)
    {
        // GUIDialogs.xml
        // LensFlares.xml
        // SurfaceFX.xml
        // TerrainDecalFX.xml
        // GraphicDetails.xml
        // DynamicTrackFX.xml
        // ShadowBlobMaterials.xml
        // TacticalCameras.xml
        // LightSources.xml
        // StarWars3DTextCrawl.xml
        // MusicEvents.xml
        // SpeechEvents.xml
        // GameConstants.xml
        // Audio.xml
        // WeatherAudio.xml
        // HeroClash.xml
        // TradeRouteLines.xml
        // RadarMap.xml
        // WeatherModifiers.xml
        // Movies.xml
        // LightningEffectTypes.xml
        // DifficultyAdjustments.xml
        // WeatherScenarios.xml
        // UnitAbilityTypes.xml
        // BlackMarketItems.xml
        // MovementClassTypeDefs.xml
        // AITerrainEffectiveness.xml


        // CONTAINER FILES:
        // GameObjectFiles.xml
        // SFXEventFiles.xml
        // CommandBarComponentFiles.xml
        // TradeRouteFiles.xml
        // HardPointDataFiles.xml
        // CampaignFiles.xml
        // FactionFiles.xml
        // TargetingPrioritySetFiles.xml
        // MousePointerFiles.xml


        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);


        var runner = new ParallelBlockingRunner(4, serviceProvider);
        runner.Error += (sender, e) =>
        {
            _logger?.LogError($"Error while initializing database '{e.Step}'");
            cts.Cancel();
        };

        runner.Run(cts.Token);


        runner.Wait();

        return Task.CompletedTask;
    }
}


public abstract class ParseXmlDatabaseStep<T> : PipelineStep where T : class
{
    public T Database { get; private set; } = null!;

    protected ParseXmlDatabaseStep(string xmlFile, GameRepository repository, IServiceProvider serviceProvider) : this([xmlFile], repository, serviceProvider)
    {
    }

    protected ParseXmlDatabaseStep(IList<string> xmlFiles, GameRepository repository, IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override void RunCore(CancellationToken token)
    {
        throw new NotImplementedException();
    }
}


public class GameRepository
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileSystem _fileSystem;
    private readonly PetroglyphDataEntryPathNormalizer _filePathNormalizer;
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
        _filePathNormalizer = serviceProvider.GetRequiredService<PetroglyphDataEntryPathNormalizer>();
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

        //var parser = new XmlObjectParser<MegaFilesXml>();
        //var megaFilesXml = parser.Parse(xmlStream)!;

        var parser = new XmlFileContainerParser();
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
            var normalizedPath = _filePathNormalizer.Normalize(filePath);
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

[XmlRoot("Mega_Files")]
public class MegaFilesXml
{

    [XmlElement(ElementName = "File")]
    public List<string> Files { get; set; }
}

public abstract class PetroglyphXmlParser<T>  : PetroglyphXmlElementParser<T>
{
    public T ParseFile(Stream xmlStream)
    { 
        var doc = XDocument.Load(xmlStream);
        var root = doc.Root;
        if (root is null)
            return default;
        return Parse(root);
    }
}


public sealed class PetroglyphXmlParserFactory
{
    public static readonly PetroglyphXmlParserFactory Instance = new();

    private PetroglyphXmlParserFactory()
    {
    }

    public IPetroglyphXmlElementParser GetParser(Type type)
    {
        if (type == typeof(string))
            return PetroglyphXmlStringParser.Instance;
        return null;
    }
}

public interface IPetroglyphXmlElementParser
{
    public object? Parse(XElement element);
}

public interface IPetroglyphXmlElementParser<out T> : IPetroglyphXmlElementParser
{
    public new T Parse(XElement element);
}


public abstract class PetroglyphXmlElementParser<T> : IPetroglyphXmlElementParser<T>
{
    protected virtual IDictionary<string, Type> Map { get; } = new Dictionary<string, Type>();

    private readonly PetroglyphXmlParserFactory _parserFactory = PetroglyphXmlParserFactory.Instance;

    public abstract T Parse(XElement element);


    public ValueListDictionary<string, object> ToKeyValuePairList(XElement element)
    {
        var keyValuePairList = new ValueListDictionary<string, object>();
        foreach (var elm in element.Elements())
        {
            var tagName = elm.Name.LocalName;

            if (!Map.ContainsKey(tagName))
                continue;

            var parser = _parserFactory.GetParser(Map[tagName]);
            var value = parser.Parse(elm);

            keyValuePairList.Add(tagName, value);
        }

        return keyValuePairList;
    }

    object? IPetroglyphXmlElementParser.Parse(XElement element)
    {
        return Parse(element);
    }
}


public class XmlFileContainerParser : PetroglyphXmlParser<XmlFileContainer>
{
    protected override IDictionary<string, Type> Map { get; } = new Dictionary<string, Type>
    {
        { "File", typeof(string) }
    };

    public override XmlFileContainer Parse(XElement element)
    {
        var xmlValues = ToKeyValuePairList(element);

        return xmlValues.TryGetValues("File", out var files)
            ? new XmlFileContainer(files.OfType<string>().ToList())
            : new XmlFileContainer([]);
    }
}

public class XmlFileContainer(IList<string> files)
{
    public IList<string> Files { get; } = files;
}


public sealed class PetroglyphXmlStringParser : PetroglyphXmlElementParser<string>
{
    public static readonly PetroglyphXmlStringParser Instance = new();

    private PetroglyphXmlStringParser()
    {
    }

    public override string Parse(XElement element)
    {
        return element.Value.Trim();
    }
}


// NOT THREAD-SAFE!
public class ValueListDictionary<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue?> _singleValueDictionary = new ();
    private readonly Dictionary<TKey, List<TValue?>> _multiValueDictionary = new();


    public bool ContainsKey(TKey key)
    {
        return _singleValueDictionary.ContainsKey(key) || _multiValueDictionary.ContainsKey(key);
    }

    public bool Add(TKey key, TValue? value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (!_singleValueDictionary.ContainsKey(key))
        {
            if (!_multiValueDictionary.TryGetValue(key, out var list))
            {
                _singleValueDictionary.Add(key, value);
                return false;
            }

            list.Add(value);
            return true;
        }

        Debug.Assert(_multiValueDictionary.ContainsKey(key) == false);

        var firstValue = _singleValueDictionary[key];
        _singleValueDictionary.Remove(key);

        _multiValueDictionary.Add(key, [
            firstValue,
            value
        ]);

        return true;
    }

    public TValue? GetLastValue(TKey key)
    {
        if (_singleValueDictionary.TryGetValue(key, out var value))
            return value;

        if (_multiValueDictionary.TryGetValue(key, out var valueList))
            return valueList.Last();

        throw new KeyNotFoundException($"The key '{key}' was not found.");
    }

    public IList<TValue?> GetValues(TKey key)
    {
        if (!TryGetValues(key, out var values))
            throw new KeyNotFoundException($"The key '{key}' was not found.");
        return values;
    }

    public bool TryGetValues(TKey key, [NotNullWhen(true)] out IList<TValue?>? values)
    {
        if (_singleValueDictionary.TryGetValue(key, out var value))
        {
            values = new List<TValue>(1)
            {
                value
            };
            return true;
        }

        if (_multiValueDictionary.TryGetValue(key, out var valueList))
        {
            values = valueList;
            return true;
        }

        values = null;
        return false;
    }

}