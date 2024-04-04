using System;
using System.Collections.Generic;
using PG.StarWarsGame.Infrastructure;

namespace RepublicAtWar.DevLauncher.Configuration;

internal class RawGermanSFXMegConfiguration(
    IPhysicalPlayableObject? physicalGameObject,
    IServiceProvider serviceProvider)
    : RawPackMegConfiguration(physicalGameObject, serviceProvider)
{
    public override IEnumerable<string> FilesToPack { get; } = new List<string>
    {
        "Data\\Audio\\Units\\German\\*.wav"
    };

    public override string FileName => "Data\\Audio\\SFX\\sfx2d_german.meg";

    public override bool FileNamesOnly => true;
}