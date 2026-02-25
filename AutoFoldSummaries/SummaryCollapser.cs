using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;

namespace AutoFoldSummaries
{
    internal sealed class SummaryCollapser
    {
        private readonly IOutliningManagerService _outliningService;
        private readonly ITextView _view;
        private int _collapsed;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private const int RetryDelayMs = 100;
        private const int MaxRetries = 30;

        public SummaryCollapser(IOutliningManagerService svc, ITextView view)
        {
            _outliningService = svc;
            _view = view;
        }

        public void Cancel()
        {
            _cts.Cancel();
        }

        public void OnGotFocus(object sender, EventArgs e)
        {
            if (!Settings.Default.Enabled) return;
            ScheduleCollapse();
        }

        public void OnRegionsChanged(object sender, RegionsChangedEventArgs e)
        {
            if (!Settings.Default.Enabled) return;
            if (_collapsed == 0) ScheduleCollapse();
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

                    var snapshot = _view.TextSnapshot;
                    var fullSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);

                    if (!snapshot.GetText().Contains("<summary>")) return;

                    int lastRegionCount = -1;
                    int stableCount = 0;
                    const int requiredStable = 15;

                    for (int attempt = 0; attempt < MaxRetries; attempt++)
                    {
                        token.ThrowIfCancellationRequested();
                        await Task.Delay(RetryDelayMs, token);
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                        if (_view.IsClosed) return;

                        var outlining = _outliningService.GetOutliningManager(_view);
                        if (outlining == null) continue;

                        int currentCount = outlining.GetAllRegions(fullSpan).Count();

                        if (currentCount == lastRegionCount && currentCount > 0)
                        {
                            stableCount++;
                        }
                        else
                        {
                            if (currentCount > 0) CollapseSummaries();
                            stableCount = 0;
                        }

                        lastRegionCount = currentCount;
                        if (stableCount >= requiredStable) return;
                    }

                    CollapseSummaries();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    ActivityLog.LogError("AutoFoldSummaries", ex.ToString());
                }
            });
        }

        private bool CollapseSummaries()
        {
            var outlining = _outliningService.GetOutliningManager(_view);
            if (outlining == null) return false;

            var snapshot = _view.TextSnapshot;
            var fullSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);

            bool foundAnySummary = false;

            Debug.WriteLine(outlining.GetAllRegions(fullSpan).Count());
            foreach (var region in outlining.GetAllRegions(fullSpan))
            {
                var text = region.Extent.GetText(snapshot);

                if (text.TrimStart().StartsWith("///") &&
                    text.Contains("<summary>"))
                {
                    foundAnySummary = true;

                    if (!region.IsCollapsed) outlining.TryCollapse(region);
                }
            }

            if (foundAnySummary) Interlocked.Exchange(ref _collapsed, 1);

            return foundAnySummary;
        }
    }
}
