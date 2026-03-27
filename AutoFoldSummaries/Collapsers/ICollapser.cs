using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;

namespace AutoFoldSummaries.Collapsers
{
    internal interface ICollapser
    {
        bool IsEnabled();
        bool HasCollapsible(ITextSnapshot snapshot);
        bool Collapse(SnapshotSpan span, ICollapsible region, IOutliningManager outlining);
    }
}
