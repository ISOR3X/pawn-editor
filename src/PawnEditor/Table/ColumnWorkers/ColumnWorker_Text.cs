using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;

[Reloadable]
public abstract class ColumnWorker_Text : ColumnWorker
{
    private static readonly NumericStringComparer comparer = new();

    protected virtual TextAnchor Anchor => TextAnchor.MiddleLeft;
    protected virtual Color CellColor => Color.white;

    public override void DoHeader(Rect rect, DefTable defTable)
    {
        base.DoHeader(rect, defTable);
        MouseoverSounds.DoRegion(rect);
    }

    public override void DoCell(Rect inRect, Def thing, DefTable defTable)
    {
        var rect1 = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
        var textFor = GetTextFor(thing);
        if (textFor == null)
            return;
        using (new TextBlock(GameFont.Small, Anchor, false))
        {
            Verse.Widgets.Label(rect1, textFor.Colorize(CellColor));
        }

        if (!Mouse.IsOver(rect1))
            return;
        var tip = GetTip(thing);
        if (tip.NullOrEmpty())
            return;
        TooltipHandler.TipRegion(rect1, (TipSignal)tip);
    }

    public override int GetMinWidth(DefTable defTable)
    {
        return Mathf.Max(base.GetMinWidth(defTable), Def.width);
    }

    public override int Compare(Def a, Def b)
    {
        return comparer.Compare(GetTextFor(a), GetTextFor(b));
    }

    public abstract string? GetTextFor(Def thing);

    protected virtual string? GetTip(Def thing)
    {
        return null;
    }
}