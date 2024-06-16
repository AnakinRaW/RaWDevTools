using System.Collections.Generic;
using PG.StarWarsGame.Engine.Language;

namespace RepublicAtWar.DevTools.Localization;

public class LocalizationFile(LanguageType language, ICollection<LocalizationEntry> entries)
{
    public LanguageType Language { get; } = language;

    public ICollection<LocalizationEntry> Entries { get; } = entries;
}