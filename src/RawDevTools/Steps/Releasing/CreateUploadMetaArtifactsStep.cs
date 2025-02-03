using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using System.Threading;
using AET.Modinfo.Model;
using AET.Modinfo.Spec;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Semver;

namespace RepublicAtWar.DevTools.Steps.Release;

public class CreateUploadMetaArtifactsStep(IServiceProvider serviceProvider) : SynchronizedStep(serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(CreateUploadMetaArtifactsStep));

    private readonly IDictionary<string, string> _replacementVariables = new Dictionary<string, string>();

    internal string? SteamTitle { get; private set; }

    internal string? SteamJsonName { get; private set; }

    protected override void RunSynchronized(CancellationToken token)
    {
        _logger?.LogInformation("Creating Modinfo, Steam json and splashes...");

        var version = SemVersion.Parse(_fileSystem.File.ReadAllText("version.txt"), SemVersionStyles.Strict);
        _replacementVariables.Add("version", version.ToString());
        _replacementVariables.Add("version-minor", ToMinorOnly(version));

        var baseInfo = ModinfoData.Parse(_fileSystem.File.ReadAllText("modinfo-base.json"));
        
        IModinfo releaseInfo;
        string steamDescription;
        if (version.IsPrerelease)
        {
            Console.WriteLine("Building a preview version!!!");
            Console.WriteLine("Building a preview version!!!");
            Console.WriteLine("Building a preview version!!!");

            releaseInfo = ModinfoData.Parse(_fileSystem.File.ReadAllText("modinfo-beta.json"));
            steamDescription = _fileSystem.File.ReadAllText("SteamText-Beta.txt");
            _fileSystem.File.Copy("splash-beta.png", "splash.png", true);
        }
        else
        {
            releaseInfo = ModinfoData.Parse(_fileSystem.File.ReadAllText("modinfo-stable.json"));
            steamDescription = _fileSystem.File.ReadAllText("SteamText-Stable.txt");
            _fileSystem.File.Copy("splash-stable.png", "splash.png", true);
        }

        if (releaseInfo.SteamData is null)
            throw new InvalidOperationException("SteamData of release modinfo data must not be null");


        var steamDescriptionWithVersion = ReplaceVariables(steamDescription, _replacementVariables);

        var steamDataWithDescription = new SteamData(releaseInfo.SteamData)
        {
            Description = steamDescriptionWithVersion
        };

        var combined = new ModinfoData(baseInfo)
        {
            SteamData = steamDataWithDescription,
            Version = version
        };

        SteamTitle = combined.SteamData.Title;
        SteamJsonName = $"{SteamTitle}.workshop.json";

        _fileSystem.File.WriteAllText("modinfo.json", combined.ToJson());
        _fileSystem.File.WriteAllText(SteamJsonName, combined.SteamData.ToJson());

        _logger?.LogInformation("Finish build release artifacts");
    }

    private string ToMinorOnly(SemVersion version)
    {
        return $"{version.Major}.{version.Minor}";
    }


    private static string ReplaceVariables(string input, IDictionary<string, string> variables)
    {
        return Regex.Replace(input,
            @"\$\{\{(.*?)\}\}",
            match => variables.TryGetValue(match.Groups[1].Value, out var value)
                ? value
                : throw new InvalidOperationException("unable to find variable to replace"));
    }
}