using System.Collections.Generic;
using System.Linq;
using AutoFoldSummaries.Collapsers;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;

namespace AutoFoldSummaries
{
    internal class CompositeCollapser
    {
        private readonly IEnumerable<ICollapser> _collapsers;

        public CompositeCollapser(IEnumerable<ICollapser> collapsers)
        {
            _collapsers = collapsers;
        }

        public bool AnyEnabled()
        {
            return _collapsers.Any(c => c.IsEnabled());
        }

        public bool AnyToCollapse(ITextSnapshot snapshot)
        {
            return _collapsers.Any(c => c.IsEnabled() && c.HasCollapsible(snapshot));
        }

        public void Collapse(IOutliningManager outlining, ITextView view)
        {
            var snapshot = view.TextSnapshot;
            var fullSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);

            var collapsers = _collapsers.Where(c => c.IsEnabled());
            foreach (var region in outlining.GetAllRegions(fullSpan))
            {
                if (region.IsCollapsed) continue;

                var regionSpan = region.Extent.GetSpan(snapshot);
                var startLine = snapshot.GetLineFromPosition(regionSpan.Start);
                var fullRegionSpan = new SnapshotSpan(startLine.Start, regionSpan.End);

                bool collapsed = false;
                foreach (var collapser in collapsers)
                {
                    collapsed = collapser.Collapse(fullRegionSpan, region, outlining);
                }
                if (collapsed) continue;
            }

        }
    }
}
