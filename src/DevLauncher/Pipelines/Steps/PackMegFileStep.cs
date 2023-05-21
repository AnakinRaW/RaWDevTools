using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using RepublicAtWar.DevLauncher.Configuration;
using RepublicAtWar.DevLauncher.Services;
using Validation;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps;

internal class PackMegFileStep : SynchronizedStep
{
    private readonly IPackMegConfiguration _config;

    public PackMegFileStep(IPackMegConfiguration config, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Requires.NotNull(config, nameof(config));
        _config = config;
    }

    protected override void SynchronizedInvoke(CancellationToken token)
    {
        using var packer = Services.GetRequiredService<IMegPackerService>();
        packer.Pack(_config);
    }
}