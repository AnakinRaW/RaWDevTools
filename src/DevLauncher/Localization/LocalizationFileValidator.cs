using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RepublicAtWar.DevLauncher.Localization;

internal class LocalizationFileValidator
{
    private readonly ValidationKind _validationKind;


    private readonly ILogger? _logger;


    // These keys from the vanilla games cause warnings.
    // Since we don't want to change 'correct' them, just mute the error
    private static readonly List<string> SuppressedKeys =
    [
        "Hint_IDC_CUSTOM_CAMPAIGN_BUTTON",
        "Hint_IDC_NEW_CAMPAIGN_BUTTON",
        "Hint_IDC_NEW_CAMPAIGN_BUTTON_DEMO",
        "Hint_IDC_NEW_FIGHT_BUTTON",
        "Hint_IDC_SKIRMISH_GAMES_BUTTON",
        "Hint_IDC_SKIRMISH_GAMES_BUTTON_DEMO",
        "MBEM02_An_Organized_Resistance.TED",
        "MBEM03_The_Pirate_Menace.TED",
        "MBEM04_Space_Planet_Geonosis.TED",
        "MBEM05_Attack_on_Mon_Calamari.TED",
        "MBEM06_Trouble_on_Kashyyyk.TED",
        "MBEM09_A_New_Weapon_of_War.TED",
        "MBEM13_Errand_with_the_Emperor.TED",
        "MBEM14_Capturing_a_Princess.TED",
        "MBEM15_The_Destruction_of_Alderaan.TED",
        "MBRM01_Space_Planet_Kuat.TED",
        "MBRM02_Interpreting_the_Network.TED",
        "MBRM03_Theft_of_the_X-wing.TED",
        "MBRM04_Kessel_Rescue",
        "MBRM06_Imperial_Liberation",
        "MBRM07_Highest_Bidder",
        "MBRM07A_Rescue_Falcon",
        "MBRM08A_Cargo_Woes",
        "MBRM09_Borrowed_Time",
        "MBRM09_Lost_and_Found",
        "MBTM00_Galactic",
        "MBTM01_Gaining_Ground",
        "MBTM02_Scramble",
        "MBTM03_Empire_at_War",
        "MBTM04_Imperial_Noose",
        "MBTM05_Imperial_Policy",
        "MTEM02_An_Organized_Resistance.TED",
        "MTEM03_The_Pirate_Menace.TED",
        "MTEM04_Space_Planet_Geonosis.TED",
        "MTEM05_Attack_on_Mon_Calamari.TED",
        "MTEM06_Trouble_on_Kashyyyk.TED",
        "MTEM09_A_New_Weapon_of_War.TED",
        "MTEM13_Errand_with_the_Emperor.TED",
        "MTEM14_Capturing_a_Princess.TED",
        "MTEM15_The_Destruction_of_Alderaan.TED",
        "MTRM01_Space_Planet_Kuat.TED",
        "MTRM02_Interpreting_the_Network.TED",
        "MTRM03_Theft_of_the_X-wing.TED",
        "MTRM04_Kessel_Rescue",
        "MTRM06_Imperial_Liberation",
        "MTRM07_Highest_Bidder",
        "MTRM07A_Rescue_Falcon",
        "MTRM08A_Cargo_Woes",
        "MTRM09_Borrowed_Time",
        "MTRM09_Lost_and_Found",
        "MTTM00_Galactic",
        "MTTM01_Gaining_Ground",
        "MTTM02_Scramble",
        "MTTM03_Empire_at_War",
        "MTTM04_Imperial_Noose",
        "MTTM05_Imperial_Policy",
        "TEXT_UW_ACT02_M07_GOAL_00h",
        "TEXT_UW_ACT02_M07_GOAL_00g",
        "TEXT_UW_ACT02_M07_GOAL_00f",
        "TEXT_UW_ACT02_M07_GOAL_00e",
        "TEXT_UW_ACT02_M07_GOAL_00d",
        "TEXT_UW_ACT02_M07_GOAL_00c",
        "TEXT_UW_ACT02_M07_GOAL_00b",
        "TEXT_TUTORIAL_CHAP_04_01a",
        "PHASE 2 LINES",
        "TEXT_COMM_ARRAY_HARD POINT",
        "TEXT_MAP_NAME_MP_LAND_27a",
        "TEXT_MAP_NAME_MP_LAND_30a",
        "TEXT_SPEECH_EHD_ENEMY_BASE_IN RANGE",
        "TEXT_STORY_AETENII_PIR_OBJECTIVE_01a",
        "TEXT_STORY_AETENII_PIR_OBJECTIVE_01b",
        "TEXT_STORY_AETENII_PIR_OBJECTIVE_01c",
        "TEXT_STORY_AETENII_PIR_OBJECTIVE_01d",
        "TEXT_STORY_ALZOCIII_INTIMIDATION_OBJECTIVE_01a",
        "TEXT_TUTORIAL_CHAP_04_01a",
        "TEXT_STORY_ALZOCIII_INTIMIDATION_OBJECTIVE_01b",
        "TEXT_STORY_ALZOCIII_INTIMIDATION_OBJECTIVE_01c",
        "TEXT_STORY_ALZOCIII_INTIMIDATION_OBJECTIVE_01d",
        "TEXT_STORY_ALZOCIII_INTIMIDATION_OBJECTIVE_01e",
        "TEXT_STORY_ALZOCIII_INTIMIDATION_OBJECTIVE_01f",
        "TEXT_STORY_ALZOCIII_INTIMIDATION_OBJECTIVE_01g",
        "TEXT_STORY_ALZOCIII_INTIMIDATION_OBJECTIVE_01h",
        "TEXT_STORY_ALZOCIII_INTIMIDATION_OBJECTIVE_01i"
    ];

    private static readonly List<string> SupportedLanguages =
        ["ENGLISH", "GERMAN", "FRENCH", "ITALIAN", "POLISH", "RUSSIAN", "SPANISH"];

    public LocalizationFileValidator(ValidationKind validationKind, IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        _validationKind = validationKind;

        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public void ValidateLanguage(string language)
    {
        if (!SupportedLanguages.Contains(language))
            ThrowOrLog($"Unrecognized language '{language}'");
    }

    public void ValidateKey(string key)
    {
        if (SuppressedKeys.Contains(key))
            return;

        if (key.StartsWith(" ") || key.EndsWith(" "))
            ThrowOrLog($"Key '{key}' has leading or trailing spaces.");

        if (key.Contains(' '))
            ThrowOrLog($"Key '{key}' should not contain spaces.");

        if (key.Contains('.'))
            ThrowOrLog($"Key '{key}' should not contain periods '.'.");

        if (key.Any(char.IsLower))
            ThrowOrLog($"Key '{key}' should have only UPPERCASE characters.");
    }

    public void ValidateValue(string key, string value)
    {
        if (value.IndexOfAny(['\r', '\n', '\t'], 0) != -1)
            ThrowOrLog($"Value of key '{key}' has invalid escape sequence.");
    }

    private void ThrowOrLog(string message)
    {
        if (_validationKind == ValidationKind.Throw)
            throw new InvalidLocalizationFileException(message);
        _logger?.LogWarning(message);
    }

    internal enum ValidationKind
    {
        Log,
        Throw
    }
}