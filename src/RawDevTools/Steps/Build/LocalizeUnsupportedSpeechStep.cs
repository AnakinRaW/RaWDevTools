using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AET.Modinfo.Spec;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Engine.Localization;
using PG.StarWarsGame.Infrastructure;
using RepublicAtWar.DevTools.Services;
using RepublicAtWar.DevTools.Steps.Settings;

namespace RepublicAtWar.DevTools.Steps.Build;

public class LocalizeUnsupportedSpeechStep(IPhysicalPlayableObject physicalGameObject, BuildSettings settings, IServiceProvider serviceProvider) 
    : PipelineStep(serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(LocalizeUnsupportedSpeechStep));
    private readonly RepublicAtWarService _republicAtWarService = new(serviceProvider);
    private readonly IGameLanguageManager _gameLanguageManager = serviceProvider.GetRequiredService<IGameLanguageManagerProvider>()
        .GetLanguageManager(GameEngineType.Foc);

    protected override Task RunCoreAsync(CancellationToken token)
    {
        return Task.Run(() =>
        {
            var languages = _republicAtWarService.GetSupportedLanguages().ToList();
            var englishSpeechDir = _fileSystem.Path.Combine(physicalGameObject.Directory.FullName, "Data\\Audio\\Speech\\English");

            if (!_fileSystem.Directory.Exists(englishSpeechDir))
                throw new InvalidOperationException("English speech directory not found. Skipping localization.");

            foreach (var (language, support) in languages)
            {
                if (language == LanguageType.English)
                    continue;

                if (support.HasFlag(LanguageSupportLevel.Speech))
                    continue;

                LocalizeSpeechFromEnglish(language, englishSpeechDir);
            }
        }, CancellationToken.None);
    }

    private void LocalizeSpeechFromEnglish(LanguageType language, string englishSpeechDir)
    {
        var targetDir = _fileSystem.Path.Combine(physicalGameObject.Directory.FullName, "Data\\Audio\\Speech", language.ToString());
        
        _logger?.LogInformation("Localizing speech for language {Language} using English language files...", language);

        var overwriteOption = settings.CleanBuild
            ? DirectoryOverwriteOption.CleanOverwrite
            : DirectoryOverwriteOption.MergeOverwrite;

        _fileSystem.DirectoryInfo.New(englishSpeechDir).Copy(targetDir, null, overwriteOption);

        var speechFiles = _fileSystem.Directory.GetFiles(targetDir, "*.mp3");
        foreach (var speechFile in speechFiles)
        {
            var fileName = _fileSystem.Path.GetFileName(speechFile);
            var localizedFileName = _gameLanguageManager.LocalizeFileName(fileName, language, out var localized);

            if (!localized)
                continue;

            var targetPath = _fileSystem.Path.Combine(targetDir, localizedFileName);
            _fileSystem.File.Move(speechFile, targetPath);
        }
        
        _logger?.LogInformation("Finished localizing speech for language {Language}", language);
    }
}
