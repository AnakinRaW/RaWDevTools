﻿using System;
using System.Collections.Generic;
using PG.StarWarsGame.Infrastructure;

namespace RepublicAtWar.DevTools.Steps.Build.Meg.Config;

public class RawCustomMapsPackMegConfiguration(
    IPhysicalPlayableObject physicalGameObject,
    IServiceProvider serviceProvider)
    : RawPackMegConfiguration(physicalGameObject, serviceProvider)
{
    public override IEnumerable<string> FilesToPack { get; } = new List<string>
    {
        "Data\\CUSTOMMAPS\\*.ted"
    };

    public override string FileName => "Data\\MPMaps.meg";
}