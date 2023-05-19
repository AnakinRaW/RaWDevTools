using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;

namespace RepublicAtWar.DevLauncher.Steps;

internal class PackMegFileStep : SynchronizedStep
{
    public PackMegFileStep(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override void SynchronizedInvoke(CancellationToken token)
    {
    }
}