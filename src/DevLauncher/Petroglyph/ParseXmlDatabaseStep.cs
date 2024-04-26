using System;
using System.Collections.Generic;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using RepublicAtWar.DevLauncher.Petroglyph.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph;

public abstract class ParseXmlDatabaseStep<T>(
    IList<string> xmlFiles,
    GameRepository repository,
    IServiceProvider serviceProvider)
    : PipelineStep(serviceProvider)
    where T : class
{
    public T Database { get; private set; } = null!;

    protected ParseXmlDatabaseStep(string xmlFile, GameRepository repository, IServiceProvider serviceProvider) : this([xmlFile], repository, serviceProvider)
    {
    }

    protected override void RunCore(CancellationToken token)
    {
        var parsedDatabaseEntries = new List<T>();
        foreach (var xmlFile in xmlFiles)
        {
            using var fileStream = repository.OpenFile(xmlFile);

            var parser = PetroglyphXmlParserFactory.Instance.GetFileParser<T>(Services);
            var parsedData = parser.ParseFile(fileStream)!;

            parsedDatabaseEntries.Add(parsedData);
        }
        Database = CreateDataBase(parsedDatabaseEntries);
    }

    protected abstract T CreateDataBase(IList<T> parsedDatabaseEntries);
}