using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines.Steps;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class BuildAndRunPipeline : SequentialPipeline
{
    private readonly ILogger? _logger;

    private readonly BuildAndRunOption _options;
    private readonly IPhysicalMod _republicAtWar;
    private readonly IServiceProvider _serviceProvider;

    public BuildAndRunPipeline(BuildAndRunOption options, IPhysicalMod republicAtWar, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _options = options;
        _republicAtWar = republicAtWar ?? throw new ArgumentNullException(nameof(republicAtWar));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }
    
    protected override Task<IList<IStep>> BuildSteps()
    {
        return Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new RunPipelineStep(new RawBuildPipeline(_options, _republicAtWar, _serviceProvider), _serviceProvider),
            new LaunchStep(_options, _republicAtWar, _serviceProvider)
        });
    }
}