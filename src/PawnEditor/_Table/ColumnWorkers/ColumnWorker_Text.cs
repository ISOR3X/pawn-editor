using HotSwap;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;

[HotSwappable]
public abstract class ColumnWorker_Text<T> : ColumnWorker<T> where T : class
{
    private static readonly NumericStringComparer comparer = new();

    protected virtual TextAnchor RowLabelAlignment => TextAnchor.MiddleLeft;
    protected virtual Color CellColor => Color.white;

    public override void DoHeader(Rect rect, TableWorker<T> table)  
    {
        base.DoHeader(rect, table);
        MouseoverSounds.DoRegion(rect);
    }

    public override void DoCell(Rect inRect, T thing, TableWorker<T> table)
    {
        var textFor = GetTextFor(thing);
        if (textFor == null)
            return;
        using (new TextBlock(GameFont.Small, RowLabelAlignment, false))
        {
            Verse.Widgets.Label(inRect, textFor.Colorize(CellColor));
        }

        if (!Mouse.IsOver(inRect))
            return;
        var tip = GetTip(thing);
        if (tip.NullOrEmpty())
            return;
        TooltipHandler.TipRegion(inRect, (TipSignal)tip);
    }
    
    public override int Compare(T a, T b)
    {
        return comparer.Compare(GetTextFor(a), GetTextFor(b));
    }

    public abstract string? GetTextFor(T thing);

    protected virtual string? GetTip(T thing)
    {
        return GetTextFor(thing);
    }
}