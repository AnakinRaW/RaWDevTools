using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using AET.ModVerify;
using AET.ModVerify.Steps;
using Microsoft.Extensions.DependencyInjection;
using PG.Commons.Hashing;
using PG.StarWarsGame.Engine;
using PG.StarWarsGame.Engine.Database;
using PG.StarWarsGame.Engine.DataTypes;
using PG.StarWarsGame.Files.MEG.Services.Builder.Normalization;
#if NETSTANDARD2_0
using AnakinRaW.CommonUtilities.FileSystem;
#endif

namespace RepublicAtWar.DevTools.Steps.Verify;

public class VerifyAllAudioStep(IGameDatabase gameDatabase, GameVerifySettings settings, IServiceProvider serviceProvider) 
    : GameVerificationStep(gameDatabase, settings, serviceProvider)
{
    public const string SampleNotFound = "WAV00";
    public const string FilePathTooLong = "WAV01";
    public const string SampleNotPCM = "WAV02";
    public const string SampleNotMono = "WAV03";
    public const string InvalidSampleRate = "WAV04";
    public const string InvalidBitsPerSeconds = "WAV05";

    private readonly PetroglyphDataEntryPathNormalizer _pathNormalizer = new(serviceProvider);
    private readonly ICrc32HashingService _hashingService = serviceProvider.GetRequiredService<ICrc32HashingService>();
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();


    protected override string LogFileName => "AudioFiles";
    public override string Name => "Audio Files";
    protected override void RunVerification(CancellationToken token)
    {
        var sampleSet = new HashSet<Crc32>();
        Span<char> sampleNameBuffer = stackalloc char[PGConstants.MaxPathLength];

        foreach (var sfxEvent in Database.SfxEvents.Entries)
        {
            foreach (var sample in sfxEvent.AllSamples)
            {
                if (sample.Length > PGConstants.MaxPathLength)
                {
                    AddError(VerificationError.Create(FilePathTooLong, $"Sample name '{sample}' is too long."));
                    continue;
                }
                var i = _pathNormalizer.Normalize(sample.AsSpan(), sampleNameBuffer);
                var sampleName = sampleNameBuffer.Slice(0, i);
                var crc = _hashingService.GetCrc32(sampleName, PGConstants.PGCrc32Encoding);

                if (!sampleSet.Add(crc))
                    continue;

                using var sampleStream = Repository.TryOpenFile(sample);
                if (sampleStream is null)
                {
                    AddError(VerificationError.Create(SampleNotFound, $"Audio file '{sample}' could not be found."));
                    continue;
                }
                using var binaryReader = new BinaryReader(sampleStream);

                // Skip Header + "fmt "
                binaryReader.BaseStream.Seek(16, SeekOrigin.Begin);

                var fmtSize = binaryReader.ReadInt32();
                var format = (WaveFormats)binaryReader.ReadInt16();
                var channels = binaryReader.ReadInt16();

                var sampleRate = binaryReader.ReadInt32();
                var bytesPerSecond = binaryReader.ReadInt32();

                var frameSize = binaryReader.ReadInt16();
                var bitPerSecondPerChannel = binaryReader.ReadInt16();

                if (format != WaveFormats.PCM)
                {
                    AddError(VerificationError.Create(SampleNotPCM, $"Audio file '{sample}' has an invalid format '{format}'. Supported is {WaveFormats.PCM}"));
                }

                if (channels > 1 && !IsAmbient2D(sfxEvent))
                {
                    AddError(VerificationError.Create(SampleNotMono, $"Audio file '{sample}' is not mono audio."));
                }

                if (sampleRate > 48_000)
                {
                    AddError(VerificationError.Create(InvalidSampleRate, $"Audio file '{sample}' has a too high sample rate of {sampleRate}. Maximum is 48.000Hz."));
                }

                if (bitPerSecondPerChannel != 16)
                {
                    AddError(VerificationError.Create(InvalidBitsPerSeconds, $"Audio file '{sample}' has an invalid bit size of {bitPerSecondPerChannel}. Supported are 16bit."));
                }

            }
        }
    }


    // Some heuristics whether a SFXEvent is most likely to be an ambient sound.
    private bool IsAmbient2D(SfxEvent sfxEvent)
    {
        if (!sfxEvent.Is2D)
            return false;

        if (sfxEvent.IsPreset)
            return false;

        // If the event is located in SFXEventsAmbient.xml we simply assume it's an ambient sound.
        var fileName = _fileSystem.Path.GetFileName(sfxEvent.Location.XmlFile.AsSpan());
        if (fileName.Equals("SFXEventsAmbient.xml".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrEmpty(sfxEvent.UsePresetName))
            return false;

        if (sfxEvent.UsePresetName!.StartsWith("Preset_AMB_2D"))
            return true;

        return true;
    }

    private enum WaveFormats
    {
        PCM = 1,
        MSADPCM = 2,
        IEEE_Float = 3,
    }
}