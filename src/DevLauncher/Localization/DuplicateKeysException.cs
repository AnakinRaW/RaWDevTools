using System.Collections.Generic;
using System.Text;

namespace RepublicAtWar.DevLauncher.Localization;

internal class DuplicateKeysException(ISet<string> keys) : InvalidLocalizationFileException
{
    public override string Message
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendLine("The localization data has the following keys duplicates:");
            foreach (var key in keys) 
                sb.AppendLine(key);
            return sb.ToString();
        }
    }
}