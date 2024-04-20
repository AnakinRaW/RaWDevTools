using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph;

internal class InitializeGameDatabasePipeline(GameRepository repository, IServiceProvider serviceProvider) : ParallelProducerConsumerPipeline(4, true, serviceProvider)
{
    private ParseGameConstantsStep _parseGameConstants = null!;

    public GameDatabase GameDatabase { get; private set; } = null!;


    protected async override IAsyncEnumerable<IStep> BuildSteps()
    {
        yield return _parseGameConstants = new ParseGameConstantsStep("DATA\\XML\\GAMECONSTANTS.XML", repository, ServiceProvider);
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
        protected override GameConstants CreateDataBase(List<GameConstants> parsedDatabaseEntries)
        {
            if (parsedDatabaseEntries.Count != 1)
                throw new InvalidOperationException("There can be only one GameConstant model.");

            return parsedDatabaseEntries.First();
        }
    }
}