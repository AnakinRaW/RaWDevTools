using System.Collections.Generic;
using System.Text;
using PG.StarWarsGame.Engine.Localization;

namespace RepublicAtWar.DevTools.Localization;

internal class DuplicateKeysException(LanguageType language, ISet<string> keys) : InvalidLocalizationFileException
{
    public override string Message
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendLine($"The localization data for '{language}' has the following keys duplicates:");
            foreach (var key in keys) 
                sb.AppendLine(key);
            return sb.ToString();
        }
    }
}