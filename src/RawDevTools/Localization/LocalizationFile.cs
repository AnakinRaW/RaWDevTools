using System.Collections.Generic;
using AnakinRaW.CommonUtilities;

namespace RepublicAtWar.DevTools.Localization;

public class LocalizationFile
{
    public string Language { get; }

    public ICollection<LocalizationEntry> Entries { get; }

    public LocalizationFile(string language, ICollection<LocalizationEntry> entries)
    {
        ThrowHelper.ThrowIfNullOrEmpty(language);
        Language = language;
        Entries = entries;
    }
}