using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.Table;
using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class ThingColumnWorker_Stuff : ThingColumnWorker
{
    public override bool Sortable => true;

    private static bool IsStuffable(Thing thing) => thing.def.stuffCategories != null;

    public override int Compare(Thing a, Thing b)
        => string.Compare(a.Stuff?.LabelCap, b.Stuff?.LabelCap, StringComparison.CurrentCultureIgnoreCase);

    public override void DrawCell(TaffyBuilder grid, Thing row)
    {
        grid.Item(draw: r =>
        {
            if (!IsStuffable(row) || row.Stuff == null) return;
            var stuffDef = row.Stuff;
            Widgets.DefIcon(r.TakeLeftPart(r.height).ContractedBy(4f), stuffDef, scale: 1f);
            r.Indent();
            using (new TextBlock(TextAnchor.MiddleLeft))
                Widgets.Label(r, stuffDef.LabelCap.Colorize(ColoredText.SubtleGrayColor));
        });
    }
}
