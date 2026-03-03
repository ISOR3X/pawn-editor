using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class ColumnWorker_Icon : ColumnWorker
{
    protected virtual int Width => 26;

    protected virtual int Padding => 2;

    public override void DoCell(Rect inRect, Def thing, DefTable defTable)
    {
        Texture2D iconFor = GetIconFor(thing);
        if (!(iconFor != null))
            return;
        Vector2 iconSize = GetIconSize(thing);
        int num1 = (int)((inRect.width - (double)iconSize.x) / 2.0);
        int num2 = Mathf.Max((int)((30.0 - iconSize.y) / 2.0), 0);
        Rect rect1 = new Rect(inRect.x + num1, inRect.y + num2, iconSize.x, iconSize.y);
        GUI.color = GetIconColor(thing);
        GUI.DrawTexture(rect1.ContractedBy(Padding), iconFor);
        GUI.color = Color.white;
        if (Mouse.IsOver(rect1))
        {
            string iconTip = GetIconTip(thing);
            if (!iconTip.NullOrEmpty())
                TooltipHandler.TipRegion(rect1, (TipSignal)iconTip);
        }

        if (Widgets.ButtonInvisible(rect1, false))
            ClickedIcon(thing);
        if (!Mouse.IsOver(rect1) || !Input.GetMouseButton(0))
            return;
        PaintedIcon(thing);
    }

    public override int GetMinWidth(DefTable defTable) => Mathf.Max(base.GetMinWidth(defTable), Width);

    public override int GetMaxWidth(DefTable defTable) => Mathf.Min(base.GetMaxWidth(defTable), GetMinWidth(defTable));

    public override int GetMinCellHeight(Def thing) => Mathf.Max(base.GetMinCellHeight(thing), Mathf.CeilToInt(GetIconSize(thing).y));

    public override int Compare(Def a, Def b) => GetValueToCompare(a).CompareTo(GetValueToCompare(b));

    private int GetValueToCompare(Def thing)
    {
        Texture2D iconFor = GetIconFor(thing);
        return !(iconFor != null) ? int.MinValue : iconFor.GetInstanceID();
    }

    protected abstract Texture2D GetIconFor(Def thing);

    protected virtual string GetIconTip(Def thing) => null;

    protected virtual Color GetIconColor(Def thing) => Color.white;

    protected virtual void ClickedIcon(Def thing)
    {
    }

    protected virtual void PaintedIcon(Def thing)
    {
    }

    protected virtual Vector2 GetIconSize(Def Def) => GetIconFor(Def) == null ? Vector2.zero : new Vector2(Width, Width);
}