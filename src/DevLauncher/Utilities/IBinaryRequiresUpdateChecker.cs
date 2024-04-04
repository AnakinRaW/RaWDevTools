using System.Collections.Generic;

namespace RepublicAtWar.DevLauncher.Utilities;

internal interface IBinaryRequiresUpdateChecker
{
    bool RequiresUpdate(string binaryFile, IEnumerable<string> files);
}