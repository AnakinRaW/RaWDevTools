using System;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline;

namespace RepublicAtWar.DevLauncher;

internal class RawDevLauncherPipeline : Pipeline
{
    public RawDevLauncherPipeline(IServiceProvider serviceProvider)
    {
        
    }

    protected override bool PrepareCore()
    {
        return true;
    }

    protected override void RunCore(CancellationToken token)
    {
    }
}