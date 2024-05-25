using System.IO;

namespace RepublicAtWar.DevLauncher.Petroglyph.Engine;

public interface IGameRepository
{
    Stream OpenFile(string filePath, bool megFileOnly = false);

    bool FileExists(string filePath, bool megFileOnly = false);

    Stream? TryOpenFile(string filePath, bool megFileOnly = false);
}