using System;
using RepublicAtWar.DevLauncher.Configuration;

namespace RepublicAtWar.DevLauncher.Services;

internal interface IMegPackerService : IDisposable
{
    void Pack(IPackMegConfiguration configuration);
}