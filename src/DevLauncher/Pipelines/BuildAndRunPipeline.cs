using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevLauncher.Pipelines.Settings;
using RepublicAtWar.DevLauncher.Pipelines.Steps;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.DevLauncher.Pipelines;

internal class BuildAndRunPipeline(
    IPhysicalMod republicAtWar,
    BuildSettings buildSettings,
    LaunchSettings launchSettings,
    IServiceProvider serviceProvider)
    : SequentialPipeline(serviceProvider)
{
    private readonly IPhysicalMod _republicAtWar = republicAtWar ?? throw new ArgumentNullException(nameof(republicAtWar));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly BuildSettings _buildSettings = buildSettings ?? throw new ArgumentNullException(nameof(buildSettings));
    private readonly LaunchSettings _launchSettings = launchSettings ?? throw new ArgumentNullException(nameof(launchSettings));

    protected override Task<IList<IStep>> BuildSteps()
    {
        return Task.FromResult<IList<IStep>>(new List<IStep>
        {
            new RunPipelineStep(new BuildPipeline(_republicAtWar, _buildSettings, _serviceProvider), _serviceProvider),
            new LaunchStep(_launchSettings, _republicAtWar, _serviceProvider)
        });
    }
}