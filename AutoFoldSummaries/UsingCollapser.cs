using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;

namespace AutoFoldSummaries
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
            foreach (var line in snapshot.Lines)
            {
                if (IsUsingLine(line.Extent)) return true;
            }
            return false;
        }

        public bool Collapse(SnapshotSpan span, ICollapsible region, IOutliningManager outlining)
        {
            if (!IsUsingLine(span)) return false;
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
    }
}
