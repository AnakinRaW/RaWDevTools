using System;

namespace RepublicAtWar.DevLauncher.Localization;

internal interface ILocalizationFileReader : IDisposable
{
    LocalizationFile Read();
}