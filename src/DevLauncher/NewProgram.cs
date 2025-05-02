using System;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.CommonUtilities.Registry.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testably.Abstractions;

namespace RepublicAtWar.DevLauncher;

public static class MainClass
{
    // In some build scenarios we cannot have the Main method in a class that inherits a type form an embedded assembly.
    // This might result in FileNotFoundExceptions when the CLR is trying to load the type that contains the Main method.
    public static Task<int> Main(string[] args)
    {
        return new NewProgram().StartAsync(args);
    }
}

internal class NewProgram : SelfUpdateableAppLifecycle
{
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

    protected override async Task<int> RunAppAsync(string[] args, IServiceProvider appServiceProvider)
    {
        Console.WriteLine(string.Join(" ", args));
        Console.WriteLine($"Elevated: {CurrentProcessInfo.Current.IsElevated}");

#if NETFRAMEWORK

        var ps = appServiceProvider.GetRequiredService<IProductService>();
        var us = appServiceProvider.GetRequiredService<IUpdateService>();

        //var ip = ps.GetCurrentInstance();
        //var e = await us.CheckForUpdatesAsync();

        Console.WriteLine();
#endif


        Console.ReadLine();

        return 0;
    }

    protected override void ResetApp()
    {
    }

    protected override void CreateAppServices(IServiceCollection services)
    {
        services.MakeAppUpdateable(
            UpdatableApplicationEnvironment!, 
            sp => new CosturaApplicationProductService(ApplicationEnvironment, sp),
            sp => new JsonManifestLoader(sp));
    }
}