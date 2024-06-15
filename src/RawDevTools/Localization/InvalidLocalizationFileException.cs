using System;

namespace RepublicAtWar.DevTools.Localization;

internal class InvalidLocalizationFileException : Exception
{
    public InvalidLocalizationFileException()
    {
    }

    public InvalidLocalizationFileException(string message) : base(message)
    {
    }
}