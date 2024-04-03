using System.Collections.Generic;
using AnakinRaW.CommonUtilities;

namespace RepublicAtWar.DevLauncher.Localization;

internal class LocalizationFile
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