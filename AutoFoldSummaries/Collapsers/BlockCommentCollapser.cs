using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;

namespace AutoFoldSummaries.Collapsers
{
    internal class BlockCommentCollapser : ICollapser
    {
        private const string _blockComment = "/*";

        public bool IsEnabled()
        {
            return Settings.Default.CollapseBlockComments;
        }

        public bool HasCollapsible(ITextSnapshot snapshot)
        {
            foreach (var line in snapshot.Lines)
            {
                if (IsBlockCommentLine(line.Extent)) return true;
            }
            return false;
        }

        public bool Collapse(SnapshotSpan span, ICollapsible region, IOutliningManager outlining)
        {
            if (!IsBlockCommentLine(span)) return false;
            return outlining.TryCollapse(region) != null;
        }

        private bool IsBlockCommentLine(SnapshotSpan span)
        {
            var snapshot = span.Snapshot;
            int i = span.Start;
            int end = span.End;
            while (i < end && char.IsWhiteSpace(snapshot[i])) i++;
            if (end - i < _blockComment.Length) return false;
            for (int j = 0; j < _blockComment.Length; j++)
            {
                if (snapshot[i + j] != _blockComment[j]) return false;
            }
            return true;
        }
    }
}
