using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor.Table.ColumnWorkers;

public class TextColumnWorker<T>(
    TrackSizingFunction trackSize,
    Func<T, string> getText,
    string? header,
    Color? color,
    string? headerTip)
    : ColumnWorker<T>
{
    public override TrackSizingFunction TrackSize => trackSize;
    public override bool Sortable => true;
    protected override string? HeaderLabel => header;
    protected override string? HeaderTip => headerTip;

    public override int Compare(T a, T b)
        => string.Compare(getText(a), getText(b), StringComparison.CurrentCultureIgnoreCase);
    
    public override void DrawCell(TaffyBuilder grid, T row)
    {
        grid.Text(getText(row), color: color.GetValueOrDefault(Color.white), wrap: false,
            onHover: r =>
            {
                var text = getText(row);
                if (Text.CalcSize(text).x > r.width)
                    TooltipHandler.TipRegion(r, text);
            });
    }
}