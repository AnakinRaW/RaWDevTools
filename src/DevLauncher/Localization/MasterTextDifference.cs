using System.Collections.Generic;
using PG.StarWarsGame.Files.DAT.Data;

namespace RepublicAtWar.DevLauncher.Localization;

public class MasterTextDifference(
    ICollection<DatStringEntry> newEntries,
    ICollection<(DatStringEntry entry, string oldValue)> changedEntries,
    ISet<string> deletedKeys)
{
    public ICollection<DatStringEntry> NewEntries { get; } = newEntries;

    public ICollection<(DatStringEntry entry, string oldValue)> ChangedEntries { get; } = changedEntries;

    public ISet<string> DeletedKeys { get; } = deletedKeys;
}