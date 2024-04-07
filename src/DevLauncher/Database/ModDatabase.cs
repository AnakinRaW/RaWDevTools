using PG.StarWarsGame.Files.DAT.Data;

namespace RepublicAtWar.DevLauncher.Database;

public class ModDatabase
{
    public IDatModel EnglishLocalization { get; }

    public ModDatabase(IDatModel englishLocalization)
    {
        EnglishLocalization = englishLocalization;
    }
}