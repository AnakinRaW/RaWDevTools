using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using RepublicAtWar.DevLauncher.Configuration;
using RepublicAtWar.DevLauncher.Services;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps;

internal class PackMegFileStep(IPackMegConfiguration config, IServiceProvider serviceProvider)
    : SynchronizedStep(serviceProvider)
{
    private readonly IPackMegConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));

    protected override void SynchronizedInvoke(CancellationToken token)
    {
        using var packer = Services.GetRequiredService<IMegPackerService>();
        packer.Pack(_config);
    }
}