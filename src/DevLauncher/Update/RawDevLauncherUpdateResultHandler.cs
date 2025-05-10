using System;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Updater;

namespace RepublicAtWar.DevLauncher.Update;

internal sealed class RawDevLauncherUpdateResultHandler(
    UpdatableApplicationEnvironment applicationEnvironment,
    IServiceProvider serviceProvider)
    : ApplicationUpdateResultHandler(applicationEnvironment, serviceProvider)
{
    protected override Task HandleSuccess()
    {
        Console.WriteLine("Update completed!");
        return base.HandleSuccess();
    }

    protected override Task ShowError(UpdateResult updateResult)
    {
        Console.WriteLine($"Update failed with error: {updateResult.ErrorMessage}");
        return base.ShowError(updateResult);
    }

    protected override void RestartApplication(RestartReason reason)
    {
        Console.WriteLine("Restarting application to complete update...");
        base.RestartApplication(reason);
    }
}