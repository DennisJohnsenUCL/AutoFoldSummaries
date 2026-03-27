using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;

namespace AutoFoldSummaries.Collapsers
{
    class CommentCollapser : ICollapser
    {
        private const string _comment = "//";

        public bool IsEnabled()
        {
            return Settings.Default.CollapseComments;
        }

        public bool HasCollapsible(ITextSnapshot snapshot)
        {
            foreach (var line in snapshot.Lines)
            {
                if (IsCommentLine(line.Extent)) return true;
            }
            return false;
        }

        public bool Collapse(SnapshotSpan span, ICollapsible region, IOutliningManager outlining)
        {
            if (!IsCommentLine(span)) return false;
            return outlining.TryCollapse(region) != null;
        }

        private bool IsCommentLine(SnapshotSpan span)
        {
            var snapshot = span.Snapshot;
            int i = span.Start;
            int end = span.End;
            while (i < end && char.IsWhiteSpace(snapshot[i])) i++;
            if (end - i < _comment.Length) return false;
            for (int j = 0; j < _comment.Length; j++)
            {
                if (snapshot[i + j] != _comment[j]) return false;
            }
            return true;
        }
    }
}
