using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;

namespace AutoFoldSummaries
{
    internal sealed class CollapseScheduler
    {
        private readonly CompositeCollapser _composite;
        private readonly IOutliningManagerService _outliningService;
        private readonly ITextView _view;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private const int RetryDelayMs = 100;
        private const int MaxRetries = 30;

        public CollapseScheduler(CompositeCollapser composite, IOutliningManagerService svc, ITextView view)
        {
            _composite = composite;
            _outliningService = svc;
            _view = view;
        }

        public void Cancel()
        {
            _cts.Cancel();
        }

        public void OnGotFocus(object sender, EventArgs e)
        {
            if (_composite.AnyEnabled()) ScheduleCollapse();
        }

        private void ScheduleCollapse()
        {
            var oldCts = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
            oldCts.Cancel();
            var token = _cts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                    if (_view.IsClosed) return;

                    if (!_composite.AnyToCollapse(_view.TextSnapshot)) return;

                    var outlining = _outliningService.GetOutliningManager(_view);
                    if (outlining == null) return;

                    int lastRegionCount = -1;
                    int stableCount = 0;
                    const int requiredStable = 15;

                    for (int attempt = 0; attempt < MaxRetries; attempt++)
                    {
                        token.ThrowIfCancellationRequested();
                        await Task.Delay(RetryDelayMs, token);
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                        if (_view.IsClosed) return;

                        var snapshot = _view.TextSnapshot;
                        var fullSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);

                        int currentCount = outlining.GetAllRegions(fullSpan).Count();

                        if (currentCount == lastRegionCount && currentCount > 0)
                        {
                            stableCount++;
                        }
                        else
                        {
                            if (currentCount > 0) _composite.Collapse(outlining, _view);
                            stableCount = 0;
                        }

                        lastRegionCount = currentCount;
                        if (stableCount >= requiredStable) return;
                    }

                    _composite.Collapse(outlining, _view);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    ActivityLog.LogError("AutoFoldSummaries", ex.ToString());
                }
            });
        }
    }
}
