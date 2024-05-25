using System.Collections.Generic;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Xml;

namespace RepublicAtWar.DevLauncher.Petroglyph.Engine;

public class GameDatabase
{
    public required GameConstants GameConstants { get; init; }

    public required IList<GameObject> GameObjects { get; init; }
}