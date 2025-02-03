using System;

namespace RepublicAtWar.DevTools.Localization;

internal interface ILocalizationFileReader : IDisposable
{
    LocalizationFile Read();
}