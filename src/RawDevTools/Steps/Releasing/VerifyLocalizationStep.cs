using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Engine.Localization;
using PG.StarWarsGame.Files.DAT.Data;
using PG.StarWarsGame.Files.DAT.Files;
using PG.StarWarsGame.Files.DAT.Services;
using PG.StarWarsGame.Files.MEG.Data.Archives;
using PG.StarWarsGame.Files.MEG.Services;
using PG.StarWarsGame.Infrastructure.Mods;
using RepublicAtWar.DevTools.Services;

namespace RepublicAtWar.DevTools.Steps.Releasing;

public class VerifyLocalizationStep(
    IPhysicalMod republicAtWar,
    bool isPrerelease,
    IServiceProvider serviceProvider) : PipelineStep(serviceProvider)
{
    private readonly ILogger? _logger =
        serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(VerifyLocalizationStep));

    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly IDatFileService _datFileService = serviceProvider.GetRequiredService<IDatFileService>();
    private readonly IDatModelService _datModelService = serviceProvider.GetRequiredService<IDatModelService>();
    private readonly IMegFileService _megFileService = serviceProvider.GetRequiredService<IMegFileService>();
    private readonly RepublicAtWarService _republicAtWarService = new(serviceProvider);
    private readonly IGameLanguageManager _languageManager = serviceProvider
        .GetRequiredService<IGameLanguageManagerProvider>().GetLanguageManager(GameEngineType.Foc);
    

    protected override Task RunCoreAsync(CancellationToken token)
    {
        return Task.Run(() => RunCore(token), CancellationToken.None);
    }

    private void RunCore(CancellationToken token)
    {
        _logger?.LogInformation("Verifying localization...");

        var reportBuilder = new StringBuilder();
        var hasErrors = false;

        var supportedLanguages = _republicAtWarService.GetSupportedLanguages()
            .Select(x => x.langauge)
            .ToList();

        hasErrors |= VerifyText(reportBuilder, supportedLanguages, token);
        hasErrors |= VerifySpeech(reportBuilder, supportedLanguages, token);
        hasErrors |= VerifySfx(reportBuilder, supportedLanguages, token);

        var reportPath = _fileSystem.Path.Combine(republicAtWar.Directory.FullName, "localization_verification.txt");
        if (!hasErrors)
        {
            if (_fileSystem.File.Exists(reportPath))
                _fileSystem.File.Delete(reportPath);
            _logger?.LogInformation("Localization verification passed.");
            return;
        }

        _fileSystem.File.WriteAllText(reportPath, reportBuilder.ToString());

        LogOrThrow(reportPath);
    }

    private void LogOrThrow(string reportPath)
    {
        if (!isPrerelease)
            throw new InvalidOperationException("Localization verification failed. See localization_verification.txt for details.");
        _logger?.LogWarning("Localization verification failed. Report written to {ReportPath}", reportPath);
    }

    private bool VerifyText(
        StringBuilder reportBuilder, 
        IReadOnlyCollection<LanguageType> supportedLanguages, 
        CancellationToken token)
    {
        var hasErrors = false;
        var textDir = _fileSystem.Path.Combine(republicAtWar.Directory.FullName, "Data", "Text");
        if (!_fileSystem.Directory.Exists(textDir))
        {
            reportBuilder.AppendLine($"Text directory not found: {textDir}");
            return false;
        }

        var englishDatModel = GetDatModel(LanguageType.English);

        if (englishDatModel == null)
            throw new FileNotFoundException($"Missing required English reference file: MasterTextFile_English.dat in {textDir}");

        token.ThrowIfCancellationRequested();

        foreach (var language in supportedLanguages)
        {
            if (language == LanguageType.English)
                continue;

            var langDatModel = GetDatModel(language);

            if (langDatModel == null)
            {
                reportBuilder.AppendLine($"Missing DAT file for {language}: MasterTextFile_{language}.dat");
                hasErrors = true;
                continue;
            }

            var missingKeys = _datModelService.GetMissingKeysFromBase(englishDatModel, langDatModel).ToList();
            if (missingKeys.Any())
            {
                reportBuilder.AppendLine($"Language {language} is missing {missingKeys.Count} keys from MasterTextFile_{language}.dat:");
                foreach (var key in missingKeys)
                    reportBuilder.AppendLine($"  - {key}");
                hasErrors = true;
            }

            var additionalKeys = _datModelService.GetMissingKeysFromBase(langDatModel, englishDatModel).ToList();
            if (additionalKeys.Any())
            {
                reportBuilder.AppendLine($"Language {language} has {additionalKeys.Count} additional keys in MasterTextFile_{language}.dat (not in English):");
                foreach (var key in additionalKeys)
                    reportBuilder.AppendLine($"  - {key}");
                hasErrors = true;
            }
        }
        return hasErrors;
    }

    private IDatModel? GetDatModel(LanguageType language)
    {
        var textDir = _fileSystem.Path.Combine(republicAtWar.Directory.FullName, "Data", "Text");
        if (!_fileSystem.Directory.Exists(textDir))
            return null;

        var expectedFileName = $"MasterTextFile_{language}.dat";

        var filePath = _fileSystem.Directory.EnumerateFiles(textDir, "*.dat")
            .FirstOrDefault(f => _fileSystem.Path.GetFileName(f).Equals(expectedFileName, StringComparison.OrdinalIgnoreCase));

        return filePath == null ? null : _datFileService.LoadAs(filePath, DatFileType.OrderedByCrc32).Content;
    }

    private bool VerifySpeech(StringBuilder reportBuilder, 
        IReadOnlyCollection<LanguageType> supportedLanguages, 
        CancellationToken token)
    {
        var speechDir = _fileSystem.Path.Combine(republicAtWar.Directory.FullName, "Data", "Audio", "Speech");
        var englishSpeechDir = _fileSystem.Path.Combine(speechDir, "English");

        if (!_fileSystem.Directory.Exists(englishSpeechDir))
            throw new DirectoryNotFoundException($"Missing required English reference speech directory: {englishSpeechDir}");

        var englishFiles = _fileSystem.Directory.GetFiles(englishSpeechDir, "*", SearchOption.TopDirectoryOnly)
            .Select(f => _fileSystem.Path.GetFileName(f))
            .ToList();

        var hasErrors = VerifyEnglishFilesLocalizable(reportBuilder, englishFiles, "Speech");

        var nonMp3Files = englishFiles.Where(f => !_fileSystem.Path.GetExtension(f).Equals(".mp3", StringComparison.OrdinalIgnoreCase)).ToList();
        if (nonMp3Files.Any())
        {
            reportBuilder.AppendLine("The following English files in Speech are not .mp3 files:");
            foreach (var f in nonMp3Files)
                reportBuilder.AppendLine($"  - {f}");
            hasErrors = true;
        }

        foreach (var language in supportedLanguages)
        {
            if (language == LanguageType.English)
                continue;

            token.ThrowIfCancellationRequested();

            var langSpeechDir = _fileSystem.Path.Combine(speechDir, language.ToString());
            if (!_fileSystem.Directory.Exists(langSpeechDir))
            {
                reportBuilder.AppendLine($"Missing speech directory for {language}: {langSpeechDir}");
                hasErrors = true;
                continue;
            }

            var langFiles = new HashSet<string>(_fileSystem.Directory.GetFiles(langSpeechDir, "*", SearchOption.TopDirectoryOnly)
                .Select(f => _fileSystem.Path.GetFileName(f)), StringComparer.OrdinalIgnoreCase);

            hasErrors |= VerifyFileParity(reportBuilder, language, englishFiles, langFiles, langSpeechDir);
        }

        return hasErrors;
    }

    private bool VerifyEnglishFilesLocalizable(StringBuilder reportBuilder, IEnumerable<string> englishFiles, string category)
    {
        var nonLocalizable = englishFiles.Where(f => !_languageManager.IsFileNameLocalizable(f, true)).ToList();
        if (nonLocalizable.Any())
        {
            reportBuilder.AppendLine($"The following English files in {category} are not localizable and should be renamed:");
            foreach (var f in nonLocalizable)
                reportBuilder.AppendLine($"  - {f}");
            return true;
        }
        return false;
    }

    private bool VerifyFileParity(
        StringBuilder reportBuilder,
        LanguageType language,
        IEnumerable<string> englishFiles,
        HashSet<string> langFiles,
        string location)
    {
        var hasErrors = false;
        var missingInLang = new List<string>();
        var expectedLocalizedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var englishFile in englishFiles)
        {
            var localizedName = _languageManager.LocalizeFileName(englishFile, language, out _);
            expectedLocalizedNames.Add(localizedName);
            if (!langFiles.Contains(localizedName))
                missingInLang.Add(localizedName);
        }

        if (missingInLang.Any())
        {
            reportBuilder.AppendLine($"Language {language} has {missingInLang.Count} missing/mismatching files in {location}.");
            foreach (var missingFile in missingInLang)
                reportBuilder.AppendLine($"  - {missingFile}");
            hasErrors = true;
        }

        var additionalInLang = langFiles.Where(f => !expectedLocalizedNames.Contains(f)).ToList();
        if (additionalInLang.Any())
        {
            reportBuilder.AppendLine($"Language {language} has {additionalInLang.Count} additional files in {location} (not in English):");
            foreach (var extraFile in additionalInLang)
                reportBuilder.AppendLine($"  - {extraFile}");
            hasErrors = true;
        }

        return hasErrors;
    }

    private bool VerifySfx(StringBuilder reportBuilder, 
        IReadOnlyCollection<LanguageType> supportedLanguages, 
        CancellationToken token)
    {
        var englishMeg = GetMegArchive("Data/Audio/SFX/voices_English.meg");
        if (englishMeg == null)
            throw new FileNotFoundException("Missing required English reference SFX MEG: Data/Audio/SFX/voices_English.meg");

        return VerifyMegInternal(reportBuilder, supportedLanguages, englishMeg, l => $"Data/Audio/SFX/voices_{l}.meg", token);
    }

    private bool VerifyMegInternal(StringBuilder reportBuilder,
        IReadOnlyCollection<LanguageType> supportedLanguages,
        IMegArchive englishArchive,
        Func<LanguageType, string> megPathFunc,
        CancellationToken token)
    {
        var englishFiles = englishArchive.Select(entry => entry.Path).ToList();

        var hasErrors = VerifyEnglishFilesLocalizable(reportBuilder, englishFiles, "SFX");
        
        var nonWavFiles = englishFiles.Where(f => !_fileSystem.Path.GetExtension(f).Equals(".wav", StringComparison.OrdinalIgnoreCase)).ToList();
        if (nonWavFiles.Any())
        {
            reportBuilder.AppendLine("The following English files in SFX are not .wav files:");
            foreach (var f in nonWavFiles)
                reportBuilder.AppendLine($"  - {f}");
            hasErrors = true;
        }

        foreach (var language in supportedLanguages)
        {
            if (language == LanguageType.English)
                continue;

            token.ThrowIfCancellationRequested();

            var megPath = megPathFunc(language);
            var langArchive = GetMegArchive(megPath);

            if (langArchive == null)
            {
                reportBuilder.AppendLine($"Missing MEG file for {language}: {megPath}");
                hasErrors = true;
                continue;
            }

            var langFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in langArchive) 
                langFiles.Add(entry.Path);

            hasErrors |= VerifyFileParity(reportBuilder, language, englishFiles, langFiles, megPath);
        }

        return hasErrors;
    }

    private IMegArchive? GetMegArchive(string relativePath)
    {
        var fullPath = _fileSystem.Path.Combine(republicAtWar.Directory.FullName, relativePath);
        return !_fileSystem.File.Exists(fullPath)
            ? null
            : _megFileService.Load(fullPath).Archive;
    }
}