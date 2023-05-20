using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using RepublicAtWar.DevLauncher.Configuration;
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
        Console.WriteLine("Packing MEG");
    }
}