using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor.Table;

/// <summary>
/// Abstract base for XML-driven column workers whose rows are <see cref="Thing"/> instances.
/// Subclasses read sizing, header text, and tooltip from the <see cref="ColumnDef"/> assigned
/// by <see cref="ThingColumnDef.Worker"/> during <c>ResolveReferences</c>.
/// </summary>
public abstract class ThingColumnWorker : ColumnWorker<Thing>
{
    public required ColumnDef Def;

    public override TrackSizingFunction TrackSize => Def.ResolvedTrackSize;
    public override bool Sortable => Def.sortable;
    public override string? HeaderTip => Def.headerTip;

    public override void DrawHeader(Rect r)
    {
        if (!Def.label.NullOrEmpty())
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
                Verse.Widgets.Label(r, Def.LabelCap);
        }
        else if (Def.HeaderIcon != null)
        {
            var sz = Def.HeaderIconSize;
            var x = r.x + (r.width - sz.x) / 2f;
            GUI.DrawTexture(new Rect(x, r.yMax - sz.y, sz.x, sz.y).ContractedBy(2f), Def.HeaderIcon);
        }

        if (Def.headerTip != null)
            TooltipHandler.TipRegion(r, (TipSignal)Def.headerTip);
    }

    /// <summary>
    /// Override to draw content into the cell rect. The default <see cref="DrawCell"/> wraps
    /// this in a grid item. For full control over the grid item, override <see cref="DrawCell"/>
    /// directly instead.
    /// </summary>
    protected virtual void DrawCellContent(Rect r, Thing row) { }

    public override void DrawCell(TaffyBuilder grid, Thing row)
        => grid.Item(r => DrawCellContent(r, row));
}
