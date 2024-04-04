namespace RepublicAtWar.DevLauncher.Localization;

internal interface ILocalizationFileReader
{
    LocalizationFile ReadFile(string filePath);
}