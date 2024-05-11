using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using RepublicAtWar.DevLauncher.Petroglyph;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps.Verification;

internal abstract class ModVerificationStep(IndexAssetsAndCodeStep indexStep, IServiceProvider serviceProvider)
    : PipelineStep(serviceProvider)
{
    protected readonly IFileSystem FileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly List<string> _verifyErrors = new();

    public IReadOnlyCollection<string> VerifyErrors => _verifyErrors;

    protected GameDatabase Database { get; private set; } = null!;

    protected GameRepository Repository => Database.GameRepository;

    private StreamWriter _streamWriter = null!;

    protected sealed override void RunCore(CancellationToken token)
    {
        indexStep.Wait();
        Database = indexStep.GameDatabase;

        try
        {
            _streamWriter = CreateVerificationLogFile();
            RunVerification(token);
        }
        finally
        {
            _streamWriter.Dispose();
            _streamWriter = null!;
        }
    }

    protected abstract void RunVerification(CancellationToken token);

    protected abstract string GetLogFileName();

    protected void AddAndLogVerificationError(string errorMessage)
    {
        _verifyErrors.Add(errorMessage);
        _streamWriter.WriteLine(errorMessage);
    }

    private StreamWriter CreateVerificationLogFile()
    {
        var fileName = $"VerifyLog_{GetLogFileName()}.txt";
        var fs = FileSystem.FileStream.New(fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
        return new StreamWriter(fs);
    }
}