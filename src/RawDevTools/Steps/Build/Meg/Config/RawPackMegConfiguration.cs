﻿using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Infrastructure;

namespace RepublicAtWar.DevTools.Steps.Build.Meg.Config;

public abstract class RawPackMegConfiguration : IPackMegConfiguration
{
    protected readonly ILogger? Logger;
    protected readonly IFileSystem FileSystem;
    protected readonly IServiceProvider ServiceProvider;

    public abstract IEnumerable<string> FilesToPack { get; }

    public abstract string FileName { get; }

    public virtual bool FileNamesOnly => false;

    public IDirectoryInfo VirtualRootDirectory { get; }

    public virtual Func<string, string>? ModifyFileNameAction => null;

    protected RawPackMegConfiguration(IPhysicalPlayableObject physicalGameObject,
        IServiceProvider serviceProvider)
    {
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        FileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        VirtualRootDirectory = physicalGameObject.Directory;
    }
}