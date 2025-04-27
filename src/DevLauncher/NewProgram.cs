using System;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.External;
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

    protected override Task<int> RunAppAsync(string[] args, IServiceProvider appServiceProvider)
    {
        var exService = appServiceProvider.GetRequiredService<IExternalUpdaterService>();

        var restartOptions = exService.CreateRestartOptions(true);
        exService.Launch(restartOptions);

        Console.WriteLine(string.Join(" ", args));

        Console.ReadLine();

        return Task.FromResult(0);
    }

    protected override void ResetApp()
    {
    }

    protected override void CreateAppServices(IServiceCollection services)
    {
        services.MakeAppUpdateable(UpdatableApplicationEnvironment!, sp => new JsonManifestLoader(sp));
    }
}