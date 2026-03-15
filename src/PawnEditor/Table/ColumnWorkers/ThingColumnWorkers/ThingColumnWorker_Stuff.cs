using HotSwap;
using PawnEditor.Extensions;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class ThingColumnWorker_Stuff : ColumnWorker_Text<Thing>
{
    protected override Color CellColor => ColoredText.SubtleGrayColor;
    private static bool IsStuffable(Thing thing) => thing.def.stuffCategories != null;

    public override string? GetTextFor(Thing thing)
    {
        return !IsStuffable(thing) ? null : thing.Stuff?.LabelCap;
    }


    public override void DoCell(Rect inRect, Thing thing, TableWorker<Thing> table)
    {
        // TODO: Center content in cell.
        if (!IsStuffable(thing) || thing.Stuff == null) return;
        var stuffDef = thing.Stuff;
        Verse.Widgets.DefIcon(inRect.TakeLeftPart(inRect.height).ContractedBy(4f), stuffDef, scale: 1f);

        inRect.Indent();
        base.DoCell(inRect, thing, table);
    }
}