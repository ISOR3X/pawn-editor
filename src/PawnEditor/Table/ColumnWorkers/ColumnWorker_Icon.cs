using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class ColumnWorker_Icon : ColumnWorker
{
    protected virtual int Width => 26;

    protected virtual int Padding => 2;

    public override void DoCell(Rect inRect, Def thing, DefTable defTable)
    {
        var iconFor = GetIconFor(thing);
        if (!(iconFor != null))
            return;
        var iconSize = GetIconSize(thing);
        var num1 = (int)((inRect.width - (double)iconSize.x) / 2.0);
        var num2 = Mathf.Max((int)((30.0 - iconSize.y) / 2.0), 0);
        var rect1 = new Rect(inRect.x + num1, inRect.y + num2, iconSize.x, iconSize.y);
        using (new GUIColor(GetIconColor(thing)))
            GUI.DrawTexture(rect1.ContractedBy(Padding), iconFor);
        if (Mouse.IsOver(rect1))
        {
            var iconTip = GetIconTip(thing);
            if (!iconTip.NullOrEmpty())
                TooltipHandler.TipRegion(rect1, (TipSignal)iconTip);
        }

        if (Verse.Widgets.ButtonInvisible(rect1, false))
            ClickedIcon(thing);
        if (!Mouse.IsOver(rect1) || !Input.GetMouseButton(0))
            return;
        PaintedIcon(thing);
    }

    public override int GetMinWidth(DefTable defTable)
    {
        return Mathf.Max(base.GetMinWidth(defTable), Width);
    }

    public override int GetMaxWidth(DefTable defTable)
    {
        return Mathf.Min(base.GetMaxWidth(defTable), GetMinWidth(defTable));
    }

    public override int GetMinCellHeight(Def thing)
    {
        return Mathf.Max(base.GetMinCellHeight(thing), Mathf.CeilToInt(GetIconSize(thing).y));
    }

    public override int Compare(Def a, Def b)
    {
        return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
    }

    private int GetValueToCompare(Def thing)
    {
        var iconFor = GetIconFor(thing);
        return !(iconFor != null) ? int.MinValue : iconFor.GetInstanceID();
    }

    protected abstract Texture2D GetIconFor(Def thing);

    protected virtual string? GetIconTip(Def thing)
    {
        return null;
    }

    protected virtual Color GetIconColor(Def thing)
    {
        return Color.white;
    }

    protected virtual void ClickedIcon(Def thing)
    {
    }

    protected virtual void PaintedIcon(Def thing)
    {
    }

    protected virtual Vector2 GetIconSize(Def Def)
    {
        return GetIconFor(Def) == null ? Vector2.zero : new Vector2(Width, Width);
    }
}