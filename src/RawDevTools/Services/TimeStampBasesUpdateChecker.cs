using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace RepublicAtWar.DevTools.Services;

public class TimeStampBasesUpdateChecker(bool forceRebuild, IServiceProvider serviceProvider) : IBinaryRequiresUpdateChecker
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    public bool RequiresUpdate(string binaryFile, IEnumerable<string> files)
    {
        if (forceRebuild)
            return true;

        if (!_fileSystem.File.Exists(binaryFile))
            return true;

        var binaryTimeStamp = _fileSystem.File.GetLastWriteTimeUtc(binaryFile);

        var hasFiles = false;
        foreach (var file in files)
        {
            hasFiles = true;
            var fileTime = _fileSystem.File.GetLastWriteTimeUtc(file);
            if (fileTime > binaryTimeStamp)
                return true;
        }
        if (!hasFiles)
            return true;
        return false;
    }
}