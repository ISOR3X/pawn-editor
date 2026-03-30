using HotSwap;
using PawnEditor.Extensions;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class ColumnWorker_Icon<T> : ColumnWorker<T> where T : class
{
    protected virtual float MaxHeight => TableWorker<T>.DefaultRowHeight;

    protected Rect GetCellRect(Rect inRect, TableWorker<T> table)
    {
        var h = Mathf.Min(inRect.height, MaxHeight);
        return inRect.CenteredVertically(h).CenteredHorizontally(h);
    }

    protected virtual void DrawIcon(Rect inRect, T thing, TableWorker<T> table)
    {
        var iconFor = GetIconFor(thing);
        if (!(iconFor != null))
            return;
        using (new GUIColor(GetIconColor(thing)))
        {
            GUI.DrawTexture(inRect.ContractedBy(2f), iconFor);
        }
    }

    public override void DoCell(Rect inRect, T thing, TableWorker<T> table)
    {
        var rect1 = GetCellRect(inRect, table);
        DrawIcon(rect1, thing, table);

        if (Mouse.IsOver(rect1))
        {
            var iconTip = GetIconTip(thing);
            if (!iconTip.NullOrEmpty())
                TooltipHandler.TipRegion(rect1, (TipSignal)iconTip);
        }

        // if (Verse.Widgets.ButtonInvisible(rect1, false))
        //     ClickedIcon(thing);
        // if (Mouse.IsOver(rect1) && Input.GetMouseButton(0)) PaintedIcon(thing);
    }

    public override int Compare(T a, T b)
    {
        return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
    }

    private int GetValueToCompare(T thing)
    {
        var iconFor = GetIconFor(thing);
        return !(iconFor != null) ? int.MinValue : iconFor.GetInstanceID();
    }

    protected virtual Texture2D? GetIconFor(T thing) => null;

    protected virtual string? GetIconTip(T thing) => null;

    protected virtual Color GetIconColor(T thing) => Color.white;
}