using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using RepublicAtWar.DevLauncher.Petroglyph;
using RepublicAtWar.DevLauncher.Petroglyph.Files;
using RepublicAtWar.DevLauncher.Petroglyph.Models.Files;

namespace RepublicAtWar.DevLauncher.Pipelines.Steps.Verification;

internal class VerifyModelsTexturesAndShadersSteps(IndexAssetsAndCodeStep indexStep, IServiceProvider serviceProvider) : ModVerificationStep(indexStep, serviceProvider)
{
    private static readonly string[] TextureExtensions = ["dds", "tga"];
    private static readonly string[] ShaderExtensions = ["fx", "fxo"];

    public List<AlamoModel> Models { get; } = new();

    private readonly IChunkReaderFactory _chunkReaderFactory = serviceProvider.GetRequiredService<IChunkReaderFactory>();

    protected override void RunVerification(CancellationToken token)
    {
        var modelQueue = new Queue<string>(Database.GameObjects
            .SelectMany(x => x.Models)
            .Concat(FocHardcodedConstants.HardcodedModels));

        var visitedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (modelQueue.Count != 0)
        {
            var model = modelQueue.Dequeue();
            if (!visitedModels.Add(model))
                continue;

            token.ThrowIfCancellationRequested();

            using var modelStream = Repository.TryOpenFile(BuildModelPath(model));

            if (modelStream is null)
            {
                AddAndLogVerificationError($"Unable to find .ALO file: {model}");
                continue;
            }

            VerifyModelOrParticle(modelStream, modelQueue);
        }
    }

    protected override string GetLogFileName()
    {
        return "ModelTextureShader";
    }

    private void VerifyModelOrParticle(Stream modelStream, Queue<string> workingQueue)
    {
        using var reader = _chunkReaderFactory.GetReaderFromStream(modelStream);
        var chunkFile = reader.ReadFile();

        switch (chunkFile)
        {
            case AlamoModel model:
                VerifyModel(model, workingQueue);
                break;
            case AlamoParticle particle:
                VerifyParticle(particle);
                break;
            default:
                throw new InvalidOperationException("The data stream is neither a model nor particle.");
        }
    }

    private void VerifyParticle(AlamoParticle particle)
    {
    }

    private void VerifyModel(AlamoModel model, Queue<string> workingQueue)
    {
        foreach (var texture in model.Textures) 
            VerifyTextureExists(model, texture);

        foreach (var shader in model.Shaders) 
            VerifyShaderExists(model, shader);


        foreach (var proxy in model.Proxies)
        {
            try
            {
                var particle = FileSystem.Path.ChangeExtension(proxy, "alo");
                if (!Repository.FileExists(BuildModelPath(particle)))
                    AddAndLogVerificationError($"{model.FileName} references missing particle: {particle}");
                else
                {
                    workingQueue.Enqueue(particle);
                }
            }
            catch (Exception e)
            {
                
            }
           
        }
    }

    private void VerifyTextureExists(AlamoModel model, string texture)
    {
        if (texture == "None") 
            return;
        var texturePath = FileSystem.Path.Combine("Data/Art/Textures", texture);
        if (!Repository.FileExists(texturePath, TextureExtensions)) 
            AddAndLogVerificationError($"{model.FileName} references missing texture: {texture}");
    }

    private string BuildModelPath(string fileName)
    {
        return FileSystem.Path.Combine("Data/Art/Models", fileName);
    }

    private void VerifyShaderExists(IChunkFile file, string shader)
    {
        //var shaderPath = FileSystem.Path.Combine("Data/Art/Textures", shader);
        //if (!Repository.FileExists(shaderPath, ShaderExtensions))
        //    AddAndLogVerificationError($"{file.FileName} references missing shader: {shader}");
    }
}
