using System;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace RepublicAtWar.DevTools.PipelineSteps.Build.Meg.Config;

public interface IPackMegConfiguration
{
    IEnumerable<string> FilesToPack { get; }

    public string FileName { get; }

    public bool FileNamesOnly { get; }

    public IDirectoryInfo VirtualRootDirectory { get; }

    public Func<string, string>? ModifyFileNameAction { get; }
}