using System;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.External;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.CommonUtilities.Registry.Windows;
using AnakinRaW.ExternalUpdater.Services;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
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

        var p = new Parser(with =>
        {
            with.IgnoreUnknownArguments = true;
        });

        var parseResult = p.ParseArguments<A>(args);

        parseResult.WithParsed(o =>
        {
            Console.WriteLine(o.Value);
        });

        var other  = p.ParseArguments<B, C>(args);
        other.WithParsed<B>(b => Console.WriteLine("B: " + b.Value));
        other.WithParsed<C>(c => Console.WriteLine("C: " + c.Value));
        other.WithNotParsed(errors => Console.WriteLine("Errors: " + string.Join(", ", errors.Select(e => e.ToString()))));

#if NETFRAMEWORK

        var ps = appServiceProvider.GetRequiredService<IProductService>();
        var uh = appServiceProvider.GetRequiredService<IUpdateHandler>();
        var bm = appServiceProvider.GetRequiredService<IBranchManager>();
        var eus = appServiceProvider.GetRequiredService<IExternalUpdaterService>();
        var eul = appServiceProvider.GetRequiredService<IExternalUpdaterLauncher>();

        var bl = await bm.GetAvailableBranchesAsync();
        var ip = ps.GetCurrentInstance();
        var e = await uh.CheckForUpdateAsync(new ProductReference(ip.Name, ip.Version, bl.FirstOrDefault(x => x.IsDefault)));


        var ro = eus.CreateRestartOptions(true);
        eul.Start(eus.GetExternalUpdater(), ro);
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
            sp => new JsonManifestLoader(sp), sc =>
            {

            });
    }
}

[Verb("update")]
public class A
{
    [Option('o')]
    public string Value { get; set; }
}


[Verb("verify")]
public class B
{
    [Option('o')]
    public string Value { get; set; }
}

[Verb("other")]
public class C
{
    [Option('o')]
    public string Value { get; set; }
}