using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Engine.Localization;
using PG.StarWarsGame.Infrastructure;

namespace RepublicAtWar.DevTools.Steps.Build.Meg.Config;

public sealed class RawLocalizedSFX2DMegConfiguration : RawPackMegConfiguration
{
    private readonly Lazy<Func<string, string>?> _lazyLocalizeFileName;
    private readonly LanguageType _language;
    private readonly IGameLanguageManager _gameLanguageManager;

    private bool IsLanguageSupported { get; }

    public override Func<string, string>? ModifyFileNameAction => _lazyLocalizeFileName.Value;

    public override IEnumerable<string> FilesToPack => GetFilesToPack();

    public override string FileName => $"Data\\Audio\\SFX\\voices_{_language}.meg";

    public override bool FileNamesOnly => true;

    public RawLocalizedSFX2DMegConfiguration(LanguageType language,
        bool languageSupported,
        IPhysicalPlayableObject physicalGameObject,
        IServiceProvider serviceProvider) : base(physicalGameObject, serviceProvider)
    {
        _language = language;
        IsLanguageSupported = languageSupported;
        _gameLanguageManager = serviceProvider.GetRequiredService<IGameLanguageManagerProvider>()
            .GetLanguageManager(GameEngineType.Foc);

        _lazyLocalizeFileName = new Lazy<Func<string, string>?>(() =>
        {
            if (IsLanguageSupported)
                return null;
            return LocalizeFileName;
        });
    }


    private IEnumerable<string> GetFilesToPack()
    {
        var fs = ServiceProvider.GetRequiredService<IFileSystem>();

        var path = fs.Path.Combine("Data\\Audio\\Units\\", _language.ToString());

        if (!fs.Directory.Exists(path))
        {
            if (IsLanguageSupported)
                throw new DirectoryNotFoundException($"Unable to find SFX directory: '{path}'");

            Logger?.LogDebug($"Unsupported Language {_language} - Switching to English");
            path = $"Data\\Audio\\Units\\{LanguageType.English}";
        }

        if (!fs.Directory.Exists(path))
            throw new DirectoryNotFoundException($"Unable to find SFX directory: '{path}'");

        return new List<string>
        {
            $"{path}\\*.wav"
        };
    }

    private string LocalizeFileName(string fileName)
    {
        var newFileName = _gameLanguageManager.LocalizeFileName(fileName, _language, out var localized);
        if (!localized)
            Logger?.LogWarning($"Unable to localize file '{fileName}'");
        return newFileName;
    }
}