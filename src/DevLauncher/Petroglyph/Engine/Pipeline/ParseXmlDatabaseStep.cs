using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RepublicAtWar.DevLauncher.Petroglyph.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph.Engine.Pipeline;

public abstract class ParseXmlDatabaseStep<T>(
    IList<string> xmlFiles,
    IGameRepository repository,
    IServiceProvider serviceProvider)
    : CreateDatabaseStep<T>(repository, serviceProvider)
    where T : class
{
    protected ParseXmlDatabaseStep(string xmlFile, IGameRepository repository, IServiceProvider serviceProvider) : this([xmlFile], repository, serviceProvider)
    {
    }

    protected sealed override T CreateDatabase()
    {
        var parsedDatabaseEntries = new List<T>();
        foreach (var xmlFile in xmlFiles)
        {
            using var fileStream = GameRepository.OpenFile(xmlFile);

            var parser = PetroglyphXmlParserFactory.Instance.GetFileParser<T>(Services);
            Logger?.LogDebug($"Parsing File '{xmlFile}'");
            var parsedData = parser.ParseFile(fileStream)!;
            parsedDatabaseEntries.Add(parsedData);
        }
        return CreateDatabase(parsedDatabaseEntries);
    }

    protected abstract T CreateDatabase(IList<T> parsedDatabaseEntries);
}