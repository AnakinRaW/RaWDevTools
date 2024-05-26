using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using PG.StarWarsGame.Engine.Language;
using PG.StarWarsGame.Infrastructure;

namespace RepublicAtWar.DevLauncher.Configuration;

internal class RawLocalizedSFX2DMegConfiguration(
    string language,
    IPhysicalPlayableObject physicalGameObject,
    IServiceProvider serviceProvider) : RawPackMegConfiguration(physicalGameObject, serviceProvider)
{
    public override IEnumerable<string> FilesToPack { get; } = GetFilesToPack(language, serviceProvider);
    
    public override string FileName => $"Data\\Audio\\SFX\\sfx2d_{language}.meg";

    public override string? BaseMegFile => $"AssetLib\\Foc\\sfx2d_{language}.meg";

    public override bool FileNamesOnly => true;

    private static IEnumerable<string> GetFilesToPack(string language, IServiceProvider serviceProvider)
    {
        var fs = serviceProvider.GetRequiredService<IFileSystem>();

        var path = $"Data\\Audio\\Units\\{language}";

        if (!fs.Directory.Exists(path))
            path = $"Data\\Audio\\Units\\{LanguageType.English}";

        if (!fs.Directory.Exists(path))
            throw new DirectoryNotFoundException($"Unable to find SFX directory: '{path}'");

        return new List<string>
        {
            $"{path}\\*.wav"
        };
    }
}