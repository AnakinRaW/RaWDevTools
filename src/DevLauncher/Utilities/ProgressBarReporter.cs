using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;

namespace RepublicAtWar.DevLauncher.Utilities;

internal sealed class ProgressBarReporter : DisposableObject
{
    private readonly IProgressStep _step;
    private ProgressBar? _progressBar;

    public ProgressBarReporter(IProgressStep step)
    {
        _step = step;
        step.Progress += OnProgress;
    }

    private void OnProgress(object sender, ProgressEventArgs<object?> e)
    {
        _progressBar ??= new ProgressBar();
        _progressBar.Report(e.Progress);
        if (e.Progress >= 1.0)
        {
            _progressBar.Dispose();
            _progressBar = null;
        }
    }

    protected override void DisposeResources()
    {
        _step.Progress -= OnProgress;
        base.DisposeResources();
    }
}