using PetroGlyph.Games.EawFoc;
using System;
using System.Collections.Generic;

namespace RepublicAtWar.DevLauncher.Configuration;

internal class RawCustomMapsPackMegConfiguration(
    IPhysicalPlayableObject? physicalGameObject,
    IServiceProvider serviceProvider)
    : RawPackMegConfiguration(physicalGameObject, serviceProvider)
{
    public override IEnumerable<string> FilesToPack { get; } = new List<string>
    {
        "Data\\CUSTOMMAPS\\*.ted"
    };

    public override string FileName => "Data\\MPMaps.meg";
}