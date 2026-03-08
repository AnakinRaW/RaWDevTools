using System;
using System.Collections.Generic;
using System.Linq;
using AET.Modinfo.Spec;
using Microsoft.Extensions.DependencyInjection;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Engine.Localization;

namespace RepublicAtWar.DevTools.Services;

public sealed class RepublicAtWarService(IServiceProvider serviceProvider)
{
    private readonly IGameLanguageManager _languageManager = serviceProvider
        .GetRequiredService<IGameLanguageManagerProvider>().GetLanguageManager(GameEngineType.Foc);

    public IEnumerable<(LanguageType langauge, LanguageSupportLevel support)> GetSupportedLanguages()
    {
        var supportedLanguages = _languageManager.SupportedLanguages;

        foreach (var language in supportedLanguages)
        {
            if (language is LanguageType.English or LanguageType.German)
                yield return (language, LanguageSupportLevel.FullLocalized);
            else
                yield return (language, LanguageSupportLevel.Text);
        }
    }
}