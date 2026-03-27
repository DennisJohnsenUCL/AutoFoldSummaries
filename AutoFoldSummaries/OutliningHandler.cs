using System.Collections.Generic;
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

            var composite = new CompositeCollapser(
                new List<ICollapser>() {
                    new SummaryCollapser(),
                    new UsingCollapser(),
                    new BlockCommentCollapser()
                });
            var scheduler = new CollapseScheduler(composite, OutliningService, textView);

            textView.GotAggregateFocus += scheduler.OnGotFocus;

            textView.Closed += (s, e) =>
            {
                scheduler.Cancel();
                textView.GotAggregateFocus -= scheduler.OnGotFocus;
            };
        }
    }
}
