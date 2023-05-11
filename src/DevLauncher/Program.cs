using System;
using System.Reflection;
using AnakinRaW.ApplicationBase;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.CommonUtilities.Registry.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace RepublicAtWar.DevLauncher;

internal class Program : CliBootstrapper
{
    public static int Main(string[] args)
    {
        return new Program().Run(args);
    }

    protected override IApplicationEnvironment CreateEnvironment(IServiceProvider serviceProvider)
    {
        return new DevLauncherEnvironment(Assembly.GetExecutingAssembly(), serviceProvider);
    }

    protected override IRegistry CreateRegistry()
    {
        return new WindowsRegistry();
    }

    protected override int ExecuteAfterUpdate(string[] args, IServiceCollection serviceCollection)
    {
        var sp = serviceCollection.BuildServiceProvider();
        var env = sp.GetRequiredService<IApplicationEnvironment>();

        Console.WriteLine($"Current Version: {env.AssemblyInfo.FileVersion}");
        Console.WriteLine("Custom Tool Code would have been executed. Press enter to exit.");
        Console.ReadLine();
        return 0;
    }
}