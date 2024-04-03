using System;

namespace RepublicAtWar.DevLauncher.Localization;

internal class InvalidLocalizationFileException(string message) : Exception(message);