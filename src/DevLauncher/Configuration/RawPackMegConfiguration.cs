using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using PG.StarWarsGame.Infrastructure;

namespace RepublicAtWar.DevLauncher.Configuration;

internal abstract class RawPackMegConfiguration : IPackMegConfiguration
{
    protected readonly IFileSystem FileSystem;
    
    public abstract IEnumerable<string> FilesToPack { get; }
    public abstract string FileName { get; }
    public virtual bool FileNamesOnly => false;

    public IDirectoryInfo? VirtualRootDirectory { get; }

    protected RawPackMegConfiguration(IPhysicalPlayableObject? physicalGameObject, IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
        FileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        VirtualRootDirectory = physicalGameObject?.Directory;
    }
}