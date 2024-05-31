using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Options;
using RepublicAtWar.DevLauncher.Pipelines.Steps;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class BuildAndRunPipeline(
    BuildAndRunOption options,
    IPhysicalMod republicAtWar,
    IServiceProvider serviceProvider)
    : SequentialPipeline(serviceProvider)
{
    private readonly IPhysicalMod _republicAtWar = republicAtWar ?? throw new ArgumentNullException(nameof(republicAtWar));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    protected override Task<IList<IStep>> BuildSteps()
    {
        return Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new RunPipelineStep(new BuildPipeline(_republicAtWar, options, _serviceProvider), _serviceProvider),
            new LaunchStep(options, _republicAtWar, _serviceProvider)
        });
    }
}