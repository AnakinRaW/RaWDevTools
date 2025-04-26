using System;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase;
using AnakinRaW.ApplicationBase.New;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.CommonUtilities.Registry.Windows;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;

namespace RepublicAtWar.DevLauncher;

internal class NewProgram : SelfUpdateableAppLifecycle
{
    private static Task<int> Main(string[] args)
    {
        return new NewProgram().StartAsync(args);
    }
    
    protected override ApplicationEnvironment CreateAppEnvironment()
    {
        return new DevLauncherEnvironment(Assembly.GetExecutingAssembly(), FileSystem);
    }

    protected override IFileSystem CreateFileSystem()
    {
        return new RealFileSystem();
    }

    protected override IRegistry CreateRegistry()
    {
        return new WindowsRegistry();
    }

    protected override Task<int> RunAppAsync(string[] args, IServiceCollection coreServices)
    {
        Console.WriteLine("123");
        return Task.FromResult(0);
    }

    protected override void ResetApp()
    {
    }
}