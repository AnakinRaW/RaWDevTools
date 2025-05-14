using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Updater;
using Microsoft.Extensions.Logging;
using RepublicAtWar.DevLauncher.Utilities;
using ConsoleUtilities = AnakinRaW.ApplicationBase.ConsoleUtilities;

namespace RepublicAtWar.DevLauncher.Update;

internal sealed class RawDevLauncherUpdater(UpdatableApplicationEnvironment environment, IServiceProvider serviceProvider)
    : ApplicationUpdater(environment, serviceProvider)
{
    public async Task AutoUpdateApplication(ProductBranch branch)
    {
        using (ConsoleUtilities.HorizontalLineSeparatedBlock())
        {
            var currentAction = "checking for update";
            try
            {
                var updateCatalog = await CheckForUpdateAsync(branch);

                if (updateCatalog.Action != UpdateCatalogAction.Update)
                    return;

                currentAction = "updating";
                await UpdateAsync(updateCatalog);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"Error while {currentAction}: {e.Message}");
                Logger?.LogError(e, $"Unable to check for updates: {e.Message}");
                Console.ResetColor();
            }
        }
    }

    public override async Task<IUpdateCatalog> CheckForUpdateAsync(ProductBranch branch, CancellationToken token = default)
    {
        var updateReference = ProductService.CreateProductReference(null, branch);

        Console.WriteLine($"Checking update for {updateReference.Name}...");
        var updateCatalog = await UpdateService.CheckForUpdatesAsync(updateReference, token);

        if (updateCatalog is null)
            throw new InvalidOperationException("Update service was already doing something.");

        if (updateCatalog.Action is UpdateCatalogAction.Install or UpdateCatalogAction.Uninstall)
            throw new NotSupportedException("Install and Uninstall operations are not supported");

        if (updateCatalog.Action == UpdateCatalogAction.Update)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"New update available: Version {updateCatalog.UpdateReference.Version}");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("No update available.");
        }

        return updateCatalog;
    }

    public override async Task UpdateAsync(IUpdateCatalog updateCatalog, CancellationToken token = default)
    {
        Console.WriteLine("Updating...");
        
        UpdateResult? updateResult;
        try
        {
            using (new ProgressBar(true))
            {
                updateResult = await UpdateService.UpdateAsync(updateCatalog, token);
                if (updateResult is null)
                    throw new InvalidOperationException("Update service was already doing something.");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
        
        var resultHandler = new RawDevLauncherUpdateResultHandler(Environment, ServiceProvider);

        await resultHandler.Handle(updateResult);
    }
}