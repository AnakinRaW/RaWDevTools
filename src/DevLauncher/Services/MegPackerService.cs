using System;
using RepublicAtWar.DevLauncher.Configuration;
using Validation;

namespace RepublicAtWar.DevLauncher.Services;

internal class MegPackerService : IMegPackerService
{
    private readonly IServiceProvider _serviceProvider;

    public MegPackerService(IServiceProvider serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _serviceProvider = serviceProvider;
    }

    public void Pack(IPackMegConfiguration configuration)
    {

    }

    public void Dispose()
    {
    }
}