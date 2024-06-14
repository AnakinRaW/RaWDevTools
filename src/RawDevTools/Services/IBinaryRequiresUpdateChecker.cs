using System.Collections.Generic;

namespace RepublicAtWar.DevTools.Services;

public interface IBinaryRequiresUpdateChecker
{
    bool RequiresUpdate(string binaryFile, IEnumerable<string> files);
}