using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;

namespace AutoFoldSummaries
{
    internal class SummaryCollapser : ICollapser
    {
        private const string _docComment = "///";

        public bool IsEnabled()
        {
            return Settings.Default.CollapseSummaries;
        }

        public bool HasCollapsible(ITextSnapshot snapshot)
        {
            foreach (var line in snapshot.Lines)
            {
                if (IsSummaryLine(line.GetText())) return true;
            }
            return false;
        }

        public bool Collapse(string text, ICollapsible region, IOutliningManager outlining)
        {
            if (!IsSummaryLine(text)) return false;
            if (region.IsCollapsed) return true;
            return outlining.TryCollapse(region) != null;
        }

        private bool IsSummaryLine(string text)
        {
            int i = 0;
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            return text.AsSpan(i).StartsWith(_docComment.AsSpan());
        }
    }
}
