using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using Microsoft.Extensions.DependencyInjection;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;
using RepublicAtWar.DevLauncher.Petroglyph.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph;

internal class InitializeGameDatabasePipeline(GameRepository repository, IServiceProvider serviceProvider) : ParallelProducerConsumerPipeline(4, true, serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    private ParseGameConstantsStep _parseGameConstants = null!;
    private ParseGameObjectsStep _parseGameObjects = null!;

    public GameDatabase GameDatabase { get; private set; } = null!;


    protected override async IAsyncEnumerable<IStep> BuildSteps()
    {
        yield return _parseGameConstants = new ParseGameConstantsStep("DATA\\XML\\GAMECONSTANTS.XML", repository, ServiceProvider);

        yield return await Task.Run(() =>
        {
            var parser = PetroglyphXmlParserFactory.Instance.GetFileParser<XmlFileContainer>(ServiceProvider);
            using var gameObjectFiles = repository.OpenFile("DATA\\XML\\GAMEOBJECTFILES.XML");
            var xmlFiles = parser.ParseFile(gameObjectFiles).Files.Select(x => _fileSystem.Path.Combine("DATA\\XML", x)).ToList();
            return new ParseGameObjectsStep(xmlFiles, repository, ServiceProvider);
        });
    }

    protected override async Task RunCoreAsync(CancellationToken token)
    {
        await base.RunCoreAsync(token);

        GameDatabase = new GameDatabase(repository, ServiceProvider)
        {
            GameConstants = _parseGameConstants.Database
        };
    }

    private sealed class ParseGameConstantsStep(string xmlFile, GameRepository repository, IServiceProvider serviceProvider)
        : ParseXmlDatabaseStep<GameConstants>(xmlFile, repository, serviceProvider)
    {
        protected override GameConstants CreateDataBase(IList<GameConstants> parsedDatabaseEntries)
        {
            if (parsedDatabaseEntries.Count != 1)
                throw new InvalidOperationException("There can be only one GameConstant model.");

            return parsedDatabaseEntries.First();
        }
    }

    private sealed class ParseGameObjectsStep(
        IList<string> xmlFiles,
        GameRepository repository,
        IServiceProvider serviceProvider)
        : ParseXmlDatabaseStep<IList<GameObject>>(xmlFiles, repository, serviceProvider)
    {
        protected override IList<GameObject> CreateDataBase(IList<IList<GameObject>> parsedDatabaseEntries)
        {
            throw new NotImplementedException();
        }
    }
}