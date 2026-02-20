using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace AutoFoldSummaries
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("csharp")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class OutliningHandler : IVsTextViewCreationListener
    {
        [Import]
        internal IOutliningManagerService OutliningService { get; set; }

        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null)
                return;

            if (textView.Properties.ContainsProperty(typeof(OutliningHandler)))
                return;
            textView.Properties.AddProperty(typeof(OutliningHandler), true);

            var collapser = new SummaryCollapser(OutliningService, textView);

            textView.GotAggregateFocus += collapser.OnGotFocus;

            var outlining = OutliningService.GetOutliningManager(textView);
            if (outlining != null)
            {
                outlining.RegionsChanged += collapser.OnRegionsChanged;
            }

            textView.Closed += (s, e) =>
            {
                collapser.Cancel();
                textView.GotAggregateFocus -= collapser.OnGotFocus;
                if (outlining != null)
                    outlining.RegionsChanged -= collapser.OnRegionsChanged;
            };
        }

        private sealed class SummaryCollapser
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
                ScheduleCollapse();
            }

            public void OnRegionsChanged(object sender, RegionsChangedEventArgs e)
            {
                if (_collapsed == 0)
                    ScheduleCollapse();
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

                        var earlySnapshot = _view.TextSnapshot;
                        if (!earlySnapshot.GetText().Contains("<summary>")) return;

                        for (int attempt = 0; attempt < MaxRetries; attempt++)
                        {
                            token.ThrowIfCancellationRequested();

                            await Task.Delay(RetryDelayMs, token);
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);

                            if (_view.IsClosed)
                                return;

                            bool foundAny = CollapseSummaries();

                            if (foundAny) return;
                        }
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
}
