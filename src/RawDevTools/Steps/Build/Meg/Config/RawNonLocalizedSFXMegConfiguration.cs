using System;
using System.Collections.Generic;
using PG.StarWarsGame.Infrastructure;

namespace RepublicAtWar.DevTools.Steps.Build.Meg.Config;

public class RawNonLocalizedSFXMegConfiguration(
    IPhysicalPlayableObject physicalGameObject,
    IServiceProvider serviceProvider)
    : RawPackMegConfiguration(physicalGameObject, serviceProvider)
{
    public override IEnumerable<string> FilesToPack { get; } = new List<string>
    {
        "Data\\Audio\\Units\\NonLocalized\\*.wav"
    };

    public override string FileName => "Data\\Audio\\SFX\\voices_non_localized.meg";

    public override bool FileNamesOnly => true;
}