using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;
using RepublicAtWar.DevLauncher.Petroglyph.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph.Engine.Pipeline;

public abstract class ParseXmlDatabaseFromContainerStep<T>(
    string xmlFile,
    IGameRepository repository,
    IServiceProvider serviceProvider)
    : CreateDatabaseStep<T>(repository, serviceProvider)
    where T : class
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    protected sealed override T CreateDatabase()
    {
        using var containerStream = GameRepository.OpenFile(xmlFile);
        var containerParser = PetroglyphXmlParserFactory.Instance.GetFileParser<XmlFileContainer>(Services);
        Logger?.LogDebug($"Parsing container data '{xmlFile}'");
        var container = containerParser.ParseFile(containerStream);

        var xmlFiles = container.Files.Select(x => _fileSystem.Path.Combine("DATA\\XML", x)).ToList();


        var parsedDatabaseEntries = new List<T>();
        foreach (var file in xmlFiles)
        {
            using var fileStream = GameRepository.OpenFile(file);

            var parser = PetroglyphXmlParserFactory.Instance.GetFileParser<T>(Services);
            Logger?.LogDebug($"Parsing File '{file}'");
            var parsedData = parser.ParseFile(fileStream);
            parsedDatabaseEntries.Add(parsedData);
        }
        return CreateDatabase(parsedDatabaseEntries);
    }

    protected abstract T CreateDatabase(IList<T> files);
}