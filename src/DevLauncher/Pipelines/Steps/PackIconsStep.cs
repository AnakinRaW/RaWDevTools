using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RepublicAtWar.DevLauncher.Services;
using RepublicAtWar.DevLauncher.Utilities;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps;

internal class PackIconsStep(IServiceProvider serviceProvider) : PipelineStep(serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(MegPackerService));

    private string IconsDirectory => "Data\\Art\\Textures\\Icons";
    private string MtCommandBarPath => "Data\\Art\\Textures\\MT_CommandBar";
    private string MtCommandBarDataBase => "Data\\Art\\Textures\\MT_CommandBar.mtd";
    private string MtCommandBarTexture => "Data\\Art\\Textures\\MT_CommandBar.tga";
    private string DummyMasterTextFileXml => "Data\\Text\\MasterTextFile.xml";
    private string ModCompileExe => "ModCompile.exe";



    protected override void RunCore(CancellationToken token)
    {
        // TODO: Currently not required since ModTool.exe already does this
        //if (!RequiresBuild())
        //    return;

        try
        {
            _logger?.LogInformation("Creating Master Texture Database and TGA file...");

            WriteDummyMasterTextFile();
            WriteModCompile();
            CopyIcons();

            var p = Process.Start(ModCompileExe);
            p.WaitForExitAsync(token).Wait(token);

            var result = p.ExitCode;
            if (result != 0)
                throw new Win32Exception();

            _logger?.LogInformation("Finished creating Master Texture Database and TGA file.");
        }
        finally
        {
            try
            {
                _fileSystem.File.Delete(DummyMasterTextFileXml);
                _fileSystem.Directory.Delete(MtCommandBarPath);
            }
            catch (Exception e) when(e is UnauthorizedAccessException or IOException )
            {
                // Ignore
            }
        }
    }

    private void WriteModCompile()
    {
        if (_fileSystem.File.Exists(ModCompileExe))
            return;

        using var exeFile = GetResourceStream("ModCompile.exe");
        using var exeFs = _fileSystem.FileStream.New(ModCompileExe, FileMode.Create);
        exeFile.CopyTo(exeFs);
    }

    public void CopyIcons()
    {
        _fileSystem.DirectoryInfo.New(IconsDirectory).Copy(MtCommandBarPath, null, DirectoryOverwriteOption.CleanOverwrite);
    }

    private void WriteDummyMasterTextFile()
    {
        using var dummyMasterText = GetResourceStream("DummyMasterTextFile.xml");
        using var dummyMasterTextFs = _fileSystem.FileStream.New(DummyMasterTextFileXml, FileMode.Create);
        dummyMasterText.CopyTo(dummyMasterTextFs);
    }

    private bool RequiresBuild()
    {
        if (!_fileSystem.File.Exists(MtCommandBarTexture))
            return true;

        var files = _fileSystem.Directory.EnumerateFiles(IconsDirectory);

        var updateChecker = _serviceProvider.GetRequiredService<IBinaryRequiresUpdateChecker>();
        if (updateChecker.RequiresUpdate(MtCommandBarDataBase, files))
            return true;

        _logger?.LogDebug($"MasterTexture is already up to date. Skipping build.");
        return false;
    }

    private static Stream GetResourceStream(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fullResourceName = $"RepublicAtWar.DevLauncher.Resources.{resourceName}";
        return assembly.GetManifestResourceStream(fullResourceName) ?? throw new ArgumentException($"Unable to find resource '{fullResourceName}'");
    }
}