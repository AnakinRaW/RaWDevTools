using System;
using System.IO;
using System.Linq;
using System.Threading;
using RepublicAtWar.DevLauncher.Petroglyph;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps.Verification;

internal class VerifyModelsTexturesAndShadersSteps(IndexAssetsAndCodeStep indexStep, IServiceProvider serviceProvider) : ModVerificationStep(indexStep, serviceProvider)
{
    protected override void RunVerification(CancellationToken token)
    {
        var models = Database.GameObjects
            .SelectMany(x => x.Models)
            .Concat(FocHardcodedConstants.HardcodedModels)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var repo = Database.GameRepository;

        foreach (var model in models)
        {
            token.ThrowIfCancellationRequested();

            var modelPath = FileSystem.Path.Combine("Data/Art/Models", model);
            using var modelStream = repo.TryOpenFile(modelPath);

            if (modelStream is null)
            {
                AddAndLogVerificationError($"Unable to find .ALO file: {model}");
                continue;
            }

            VerifyModel(modelStream);
        }
    }

    protected override string GetLogFileName()
    {
        return "ModelTextureShader";
    }

    private void VerifyModel(Stream modelStream)
    {
    }
}