using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;

namespace AutoFoldSummaries.Collapsers
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
                if (IsSummaryLine(line.Extent)) return true;
            }
            return false;
        }

        public bool Collapse(SnapshotSpan span, ICollapsible region, IOutliningManager outlining)
        {
            if (!IsSummaryLine(span)) return false;
            return outlining.TryCollapse(region) != null;
        }

        private bool IsSummaryLine(SnapshotSpan span)
        {
            var snapshot = span.Snapshot;
            int i = span.Start;
            int end = span.End;
            while (i < end && char.IsWhiteSpace(snapshot[i])) i++;
            if (end - i < _docComment.Length) return false;
            for (int j = 0; j < _docComment.Length; j++)
            {
                if (snapshot[i + j] != _docComment[j]) return false;
            }
            return true;
        }
    }
}
