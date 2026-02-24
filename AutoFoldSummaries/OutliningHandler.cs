using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

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
    }
}
