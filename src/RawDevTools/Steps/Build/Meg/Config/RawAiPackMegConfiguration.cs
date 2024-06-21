using System;
using System.Collections.Generic;
using PG.StarWarsGame.Infrastructure;

namespace RepublicAtWar.DevTools.Steps.Build.Meg.Config;

public class RawAiPackMegConfiguration(IPhysicalPlayableObject physicalGameObject, IServiceProvider serviceProvider)
    : RawPackMegConfiguration(physicalGameObject, serviceProvider)
{
    public override IEnumerable<string> FilesToPack { get; } = new List<string>
    {
        "Data\\XML\\AI\\**\\*.xml",
        "Data\\SCRIPTS\\AI\\**\\*.lua"
    };

    public override string FileName => "Data\\AIFiles.meg";
}