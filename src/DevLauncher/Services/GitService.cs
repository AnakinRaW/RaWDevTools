using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Semver;

namespace RepublicAtWar.DevLauncher.Services;

internal class GitService
{
    private readonly Repository _repository;
    private readonly ILogger? _logger;

    public GitService(string repoPath, IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(GitService));
        _repository = new(repoPath);
        
        Fetch();

        LatestStableReleaseCommit = GetLatestStableCommit(out var version);
        LatestStableVersion = version;
    }

    public string LatestStableVersion { get; }
    public string CurrentBranch => _repository.Head.FriendlyName;

    private Commit LatestStableReleaseCommit { get; }


    public Stream? GetLatestStableVersion(string filePath, string? fallbackCommitSha)
    {
        var contentBlob = LatestStableReleaseCommit[filePath]?.Target as Blob;

        if (contentBlob is null)
        {
            var message = $"Unable to find '{filePath}' in the latest stable version '{LatestStableVersion}'.";
            if (fallbackCommitSha is not null)
                message += $"\r\nUsing fallback commit '{fallbackCommitSha}'";
            _logger?.LogInformation(message);

            if (fallbackCommitSha is not null)
            {
                // This commit introduced the localization TXT files.
                var targetCommit = _repository.Lookup<Commit>(fallbackCommitSha);
                contentBlob = targetCommit[filePath]?.Target as Blob;
            }
        }
        return contentBlob?.GetContentStream();
    }

    private Commit GetLatestStableCommit(out string version)
    {
        (SemVersion version, Tag tag) latestStable = default;

        var tags = _repository.Tags.ToList();
        foreach (var tag in tags)
        {
            if (!SemVersion.TryParse(tag.FriendlyName, SemVersionStyles.Any, out var semVer))
                continue;

            if (!semVer.IsRelease)
                continue;

            if (latestStable.version is null || latestStable.version.CompareSortOrderTo(semVer) < 0)
                latestStable = (semVer, tag);
        }

        if (latestStable.tag is null)
            throw new InvalidOperationException("Unable to get a stable release commit from tags.");

        version = latestStable.version!.ToString();
        return (Commit)latestStable.tag.PeeledTarget;
    }

    public void Fetch()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git.exe",
            Arguments = "fetch -t",
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,

            RedirectStandardOutput = true,
            RedirectStandardError = true

        };

        if (!Process.Start(startInfo)!.WaitForExit(3000))
            throw new InvalidOperationException("Unable to fetch from origin.");
    }
}