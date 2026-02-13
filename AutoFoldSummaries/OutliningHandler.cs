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

            // Prevent attaching twice to the same view
            if (textView.Properties.ContainsProperty(typeof(OutliningHandler)))
                return;
            textView.Properties.AddProperty(typeof(OutliningHandler), true);

            var collapser = new SummaryCollapser(OutliningService, textView);

            // Collapse once when the view first gets focus
            textView.GotAggregateFocus += collapser.OnGotFocus;

            // Collapse when outlining regions are rebuilt (e.g. file first opened)
            var outlining = OutliningService.GetOutliningManager(textView);
            if (outlining != null)
            {
                outlining.RegionsChanged += collapser.OnRegionsChanged;
            }

            // Clean up when the view closes
            textView.Closed += (s, e) =>
            {
                textView.GotAggregateFocus -= collapser.OnGotFocus;
                if (outlining != null)
                    outlining.RegionsChanged -= collapser.OnRegionsChanged;
            };
        }

        /// <summary>
        /// Encapsulates debouncing and collapse logic for a single text view.
        /// </summary>
        private sealed class SummaryCollapser
        {
            private readonly IOutliningManagerService _outliningService;
            private readonly ITextView _view;
            private int _collapsed;           // 1 after first successful collapse
            private int _pendingDebounce;     // guards against overlapping timers
            private const int DebounceMs = 200;

            public SummaryCollapser(IOutliningManagerService svc, ITextView view)
            {
                _outliningService = svc;
                _view = view;
            }

            public void OnGotFocus(object sender, EventArgs e)
            {
                ScheduleCollapse();
            }

            public void OnRegionsChanged(object sender, RegionsChangedEventArgs e)
            {
                // Only react to RegionsChanged until the initial collapse succeeds.
                // After that, edits shouldn't re-collapse regions the user opened.
                if (_collapsed == 0)
                    ScheduleCollapse();
            }

            private void ScheduleCollapse()
            {
                // Simple debounce: skip if one is already pending
                if (Interlocked.CompareExchange(ref _pendingDebounce, 1, 0) != 0)
                    return;

                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    // Yield + small delay to avoid running during layout
                    await System.Threading.Tasks.Task.Delay(DebounceMs);
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    Interlocked.Exchange(ref _pendingDebounce, 0);

                    if (_view.IsClosed)
                        return;

                    CollapseSummaries();
                });
            }

            private void CollapseSummaries()
            {
                var outlining = _outliningService.GetOutliningManager(_view);
                if (outlining == null) return;

                var snapshot = _view.TextSnapshot;
                var fullSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);

                bool didCollapse = false;

                foreach (var region in outlining.GetAllRegions(fullSpan))
                {
                    // Skip already-collapsed regions — avoids redundant work
                    if (region.IsCollapsed)
                        continue;

                    var text = region.Extent.GetText(snapshot);

                    if (text.TrimStart().StartsWith("///") &&
                        text.Contains("<summary>"))
                    {
                        outlining.TryCollapse(region);
                        didCollapse = true;
                    }
                }

                if (didCollapse)
                    Interlocked.Exchange(ref _collapsed, 1);
            }
        }
    }
}
