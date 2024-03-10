using System;
using System.Collections.Generic;
using PetroGlyph.Games.EawFoc;

namespace RepublicAtWar.DevLauncher.Configuration;

internal class RawNonLocalizedSFXMegConfiguration(
    IPhysicalPlayableObject? physicalGameObject,
    IServiceProvider serviceProvider)
    : RawPackMegConfiguration(physicalGameObject, serviceProvider)
{
    public override IEnumerable<string> FilesToPack { get; } = new List<string>
    {
        "Data\\Audio\\Units\\NonLocalized\\*.wav"
    };

    public override string FileName => "Data\\Audio\\SFX\\sfx2d_non_localized.meg";

    public override bool FileNamesOnly => true;
}