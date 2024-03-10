using System;
using System.Collections.Generic;
using PetroGlyph.Games.EawFoc;

namespace RepublicAtWar.DevLauncher.Configuration;

internal class RawAiPackMegConfiguration(IPhysicalPlayableObject? physicalGameObject, IServiceProvider serviceProvider)
    : RawPackMegConfiguration(physicalGameObject, serviceProvider)
{
    public override IEnumerable<string> FilesToPack { get; } = new List<string>()
    {
        "Data\\XML\\AI\\**\\*.xml",
        "Data\\SCRIPTS\\AI\\**\\*.lua"
    };

    public override string FileName => "Data\\AIFiles.meg";
}