using System.Collections.Generic;

namespace RepublicAtWar.DevLauncher.Services;

internal interface IBinaryRequiresUpdateChecker
{
    bool RequiresUpdate(string binaryFile, IEnumerable<string> files);
}