using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;

[Reloadable]
public abstract class ColumnWorker_Text<T> : ColumnWorker<T> where T : class
{
    private static readonly NumericStringComparer comparer = new();

    protected virtual TextAnchor Anchor => TextAnchor.MiddleLeft;
    protected virtual Color CellColor => Color.white;

    public override void DoHeader(Rect rect, TableWorker<T> table)
    {
        base.DoHeader(rect, table);
        MouseoverSounds.DoRegion(rect);
    }

    public override void DoCell(Rect inRect, T thing, TableWorker<T> table)
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

    public override int GetMinWidth(TableWorker<T> table)
    {
        return Mathf.Max(base.GetMinWidth(table), Def.width);
    }

    public override int Compare(T a, T b)
    {
        return comparer.Compare(GetTextFor(a), GetTextFor(b));
    }

    public abstract string? GetTextFor(T thing);

    protected virtual string? GetTip(T thing)
    {
        return null;
    }
}