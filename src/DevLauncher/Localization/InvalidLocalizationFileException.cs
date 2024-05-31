using System;

namespace RepublicAtWar.DevLauncher.Localization;

internal class InvalidLocalizationFileException : Exception
{
    public InvalidLocalizationFileException()
    {
    }

    public InvalidLocalizationFileException(string message) : base(message)
    {
    }
}