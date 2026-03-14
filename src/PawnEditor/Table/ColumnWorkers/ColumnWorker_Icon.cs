using HotSwap;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public abstract class ColumnWorker_Icon<T> : ColumnWorker<T> where T : class
{
    protected virtual int Width => 26;

    protected virtual int Padding => 2;

    protected Rect GetCellRect(T thing, Rect inRect)
    {
        var iconSize = GetIconSize(thing);
        var num1 = (int)((inRect.width - (double)iconSize.x) / 2.0);
        var num2 = Mathf.Max((int)((30.0 - iconSize.y) / 2.0), 0);
        return new Rect(inRect.x + num1, inRect.y + num2, iconSize.x, iconSize.y);
    }

    protected virtual void DrawIcon(Rect inRect, T thing, TableWorker<T> table)
    {
        var iconFor = GetIconFor(thing);
        if (!(iconFor != null))
            return;
        using (new GUIColor(GetIconColor(thing)))
        {
            GUI.DrawTexture(inRect.ContractedBy(Padding), iconFor);
        }
    }

    public override void DoCell(Rect inRect, T thing, TableWorker<T> table)
    {
        var rect1 = GetCellRect(thing, inRect);
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

    public override int GetMinWidth(TableWorker<T> table)
    {
        return Mathf.Max(base.GetMinWidth(table), Width);
    }

    public override int GetMaxWidth(TableWorker<T> table)
    {
        return Mathf.Min(base.GetMaxWidth(table), GetMinWidth(table));
    }

    public override int GetMinCellHeight(T thing)
    {
        return Mathf.Max(base.GetMinCellHeight(thing), Mathf.CeilToInt(GetIconSize(thing).y));
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
    

    // protected virtual void ClickedIcon(T thing)
    // {
    // }
    //
    // protected virtual void PaintedIcon(T thing)
    // {
    // }

    protected virtual Vector2 GetIconSize(T thing) => new Vector2(Width, Width);
}