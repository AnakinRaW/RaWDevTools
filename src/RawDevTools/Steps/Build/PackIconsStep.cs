﻿using System;
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
using RepublicAtWar.DevTools.Services;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.DevTools.Steps.Build;

public class PackIconsStep(BuildSettings settings, IServiceProvider serviceProvider) : PipelineStep(serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(PackIconsStep));

    private const string IconsDirectory = "Data\\Art\\Textures\\Icons";
    private const string MtCommandBarPath = "Data\\Art\\Textures\\MT_CommandBar";
    private const string MtCommandBarDataBase = "Data\\Art\\Textures\\MT_CommandBar.mtd";
    private const string MtCommandBarTexture = "Data\\Art\\Textures\\MT_CommandBar.tga";
    private const string DummyMasterTextFileXml = "Data\\Text\\MasterTextFile.xml";
    private const string ModCompileExe = "ModCompile.exe";

    protected override void RunCore(CancellationToken token)
    {
        if (!_fileSystem.Directory.Exists(IconsDirectory))
        {
            _logger?.LogWarning("No Icons directory found.");
            return;
        }
        
        if (settings.CleanBuild)
        {
            try
            {
                _fileSystem.File.Delete(MtCommandBarDataBase);
                _fileSystem.File.Delete(MtCommandBarTexture);
            }
            catch (IOException)
            {
                // Ignore
            }
        }

        try
        {
            _logger?.LogInformation("Creating Master Texture Database and TGA data...");

            WriteDummyMasterTextFile();
            WriteModCompile();
            CopyIcons();

            var p = Process.Start(ModCompileExe);
            p.WaitForExitAsync(token).Wait(token);

            var result = p.ExitCode;
            if (result != 0)
                throw new Win32Exception($"Process '{p.ProcessName}' failed with error: {result}");

            _logger?.LogInformation("Finished creating Master Texture Database and TGA data.");
        }
        finally
        {
            try
            {
                _fileSystem.File.Delete(DummyMasterTextFileXml);
                _fileSystem.Directory.Delete(MtCommandBarPath);
            }
            catch (Exception e) when (e is UnauthorizedAccessException or IOException)
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

        var updateChecker = new TimeStampBasesUpdateChecker(settings.CleanBuild, _serviceProvider);
        if (updateChecker.RequiresUpdate(MtCommandBarDataBase, files))
            return true;

        _logger?.LogDebug($"MasterTexture is already up to date. Skipping build.");
        return false;
    }

    private static Stream GetResourceStream(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fullResourceName = $"RepublicAtWar.DevTools.Resources.{resourceName}";
        return assembly.GetManifestResourceStream(fullResourceName) ?? throw new ArgumentException($"Unable to find resource '{fullResourceName}'");
    }
}