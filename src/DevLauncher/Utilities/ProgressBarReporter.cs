using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;

namespace RepublicAtWar.DevLauncher.Utilities;

internal sealed class ProgressBarReporter : IStepProgressReporter
{
    private ProgressBar? _progressBar;

    public void Report(IProgressStep step, double progress)
    {
        if (_progressBar is null)
            _progressBar = new ProgressBar();
        _progressBar.Report(progress);
        if (progress == 1.0)
        {
            _progressBar.Dispose();
            _progressBar = null;
        }
    }
}