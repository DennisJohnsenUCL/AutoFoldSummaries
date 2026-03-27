using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;

namespace AutoFoldSummaries
{
    internal interface ICollapser
    {
        bool IsEnabled();
        bool HasCollapsible(ITextSnapshot snapshot);
        bool Collapse(string text, ICollapsible region, IOutliningManager outlining);
    }
}
