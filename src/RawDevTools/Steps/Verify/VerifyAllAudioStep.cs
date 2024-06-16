using System;
using System.IO;
using System.Linq;
using System.Threading;
using AET.ModVerify;
using AET.ModVerify.Steps;
using PG.StarWarsGame.Engine.Database;

namespace RepublicAtWar.DevTools.PipelineSteps.Verify;

public class VerifyAllAudioStep(IGameDatabase gameDatabase, ModVerifySettings settings, IServiceProvider serviceProvider) : GameVerificationStep(gameDatabase, settings, serviceProvider)
{
    protected override string LogFileName => "AudioFiles";
    public override string Name => "Audio Files";
    protected override void RunVerification(CancellationToken token)
    {
        var allAudioFiles = Repository.FindFiles("**/*.wav").ToList();

        foreach (var audioFile in allAudioFiles)
        {
            var fileStream = Repository.OpenFile(audioFile);
            using var binaryReader = new BinaryReader(fileStream);

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
                AddError(VerificationError.Create("WAV01", $"Audio file '{audioFile}' has an invalid format '{format}'. Supported is {WaveFormats.PCM}"));
            }

            if (channels > 1)
            {
                // TODO: Ambient 2D sound are allowed to have stereo channel
                AddError(VerificationError.Create("WAV02", $"Audio file '{audioFile}' is not mono audio."));
            }

            if (sampleRate > 48_000)
            { 
                AddError(VerificationError.Create("WAV03", $"Audio file '{audioFile}' has a too high sample rate of {sampleRate}. Maximum is 48.000Hz."));
            }

            if (bitPerSecondPerChannel != 16)
            { 
                AddError(VerificationError.Create("WAV04", $"Audio file '{audioFile}' has an invalid bit size of {bitPerSecondPerChannel}. Supported are 16bit."));
            }
        }
    }

    private enum WaveFormats
    {
        PCM = 1,
        MSADPCM = 2,
        IEEE_Float = 3,
    }
}