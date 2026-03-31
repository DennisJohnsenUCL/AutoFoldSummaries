using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;

namespace AutoFoldSummaries.Collapsers
{
    internal class UsingCollapser : ICollapser
    {
        private const string _using = "using ";

        public bool IsEnabled()
        {
            return Settings.Default.CollapseUsings;
        }

        public bool HasCollapsible(ITextSnapshot snapshot)
        {
            int usingBlock = 0;
            foreach (var line in snapshot.Lines)
            {
                if (IsUsingLine(line.Extent)) usingBlock++;
                if (usingBlock >= 2) return true;
            }
            return false;
        }

        public bool Collapse(SnapshotSpan span, ICollapsible region, IOutliningManager outlining)
        {
            var snapshot = span.Snapshot;
            var startLine = snapshot.GetLineFromPosition(span.Start);
            var endLine = snapshot.GetLineFromPosition(span.End);

            for (int i = startLine.LineNumber; i <= endLine.LineNumber; i++)
            {
                var line = snapshot.GetLineFromLineNumber(i);
                if (!IsUsingLine(line.Extent) && !IsBlank(line.Extent)) return false;
            }

            return outlining.TryCollapse(region) != null;
        }

        private bool IsUsingLine(SnapshotSpan span)
        {
            var snapshot = span.Snapshot;
            int i = span.Start;
            int end = span.End;
            while (i < end && char.IsWhiteSpace(snapshot[i])) i++;
            if (end - i < _using.Length) return false;
            for (int j = 0; j < _using.Length; j++)
            {
                if (snapshot[i + j] != _using[j]) return false;
            }
            return true;
        }

        private bool IsBlank(SnapshotSpan span)
        {
            var snapshot = span.Snapshot;
            for (int i = span.Start; i < span.End; i++)
            {
                if (!char.IsWhiteSpace(snapshot[i])) return false;
            }
            return true;
        }
    }
}
