using System.Collections.Generic;
using PG.StarWarsGame.Files.DAT.Data;

namespace RepublicAtWar.DevTools.Localization;

public class MasterTextDifference(
    ICollection<DatStringEntry> newEntries,
    ICollection<(DatStringEntry baseEntry, string changedValue)> changedEntries,
    ISet<string> deletedKeys)
{
    public ICollection<DatStringEntry> NewEntries { get; } = newEntries;

    public ICollection<(DatStringEntry baseEntry, string changedValue)> ChangedEntries { get; } = changedEntries;

    public ISet<string> DeletedKeys { get; } = deletedKeys;
}