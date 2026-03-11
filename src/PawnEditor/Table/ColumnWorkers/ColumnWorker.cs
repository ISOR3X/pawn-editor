using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;

[StaticConstructorOnStartup]
public abstract class ColumnWorker
{
    protected const int DefaultCellHeight = 30;
    private static readonly Texture2D SortingIcon = ContentFinder<Texture2D>.Get("UI/Icons/Sorting");

    private static readonly Texture2D
        SortingDescendingIcon = ContentFinder<Texture2D>.Get("UI/Icons/SortingDescending");

    public required ColumnDef Def;
    protected virtual TextAnchor LabelAlignment => TextAnchor.LowerCenter;

    protected virtual Color HeaderColor => Color.white;
    protected virtual GameFont HeaderFont => GameFont.Small;

    public virtual bool VisibleCurrently => true;

    public virtual void DoHeader(Rect rect, DefTable defTable)
    {
        if (!Def.label.NullOrEmpty())
        {
            using (new TextBlock(HeaderFont, LabelAlignment, false))
            {
                var rect1 = rect;
                rect1.y += 3f;
                Verse.Widgets.Label(rect1, Def.LabelCap.Resolve().Truncate(rect.width).Colorize(HeaderColor));
            }
        }
        else if (Def.HeaderIcon != null)
        {
            var headerIconSize = Def.HeaderIconSize;
            var num = (int)((rect.width - (double)headerIconSize.x) / 2.0);
            GUI.DrawTexture(
                new Rect(rect.x + num, rect.yMax - headerIconSize.y, headerIconSize.x, headerIconSize.y)
                    .ContractedBy(2f), Def.HeaderIcon);
        }

        if (defTable.SortingBy != null && defTable.SortingBy.Equals(Def))
        {
            var image = defTable.SortingDescending ? SortingDescendingIcon : SortingIcon;
            GUI.DrawTexture(
                new Rect((float)(rect.xMax - (double)image.width - 1.0),
                    (float)(rect.yMax - (double)image.height - 1.0), image.width, image.height), image);
        }

        if (!Def.HeaderInteractable)
            return;
        var interactableHeaderRect = GetInteractableHeaderRect(rect, defTable);
        if (Mouse.IsOver(interactableHeaderRect))
        {
            Verse.Widgets.DrawHighlight(interactableHeaderRect);
            var headerTip = GetHeaderTip(defTable);
            if (!headerTip.NullOrEmpty())
                TooltipHandler.TipRegion(interactableHeaderRect, (TipSignal)headerTip);
        }

        if (!Verse.Widgets.ButtonInvisible(interactableHeaderRect))
            return;
        HeaderClicked(rect, defTable);
    }

    public abstract void DoCell(Rect inRect, Def thing, DefTable defTable);

    public virtual bool CanGroupWith(Def thing, Def other)
    {
        return false;
    }

    public virtual int GetMinWidth(DefTable defTable)
    {
        if (!Def.label.NullOrEmpty())
            // Use TextBlock to restore the previous font state rather than hardcoding Small.
            using (new TextBlock(HeaderFont))
            {
                return Mathf.CeilToInt(Text.CalcSize(Def.LabelCap).x);
            }

        return Def.HeaderIcon != null ? Mathf.CeilToInt(Def.HeaderIconSize.x) : 1;
    }

    public virtual int GetMaxWidth(DefTable defTable)
    {
        return 1000000;
    }

    public virtual int GetOptimalWidth(DefTable defTable)
    {
        return GetMinWidth(defTable);
    }

    public virtual int GetMinCellHeight(Def thing)
    {
        return (int)DefTable.DefaultRowHeight;
    }

    public virtual int GetMinHeaderHeight(DefTable defTable)
    {
        if (!Def.label.NullOrEmpty())
            // Use TextBlock to restore the previous font state rather than hardcoding Small.
            using (new TextBlock(HeaderFont))
            {
                return Mathf.CeilToInt(Text.CalcSize(Def.LabelCap).y);
            }

        return Def.HeaderIcon != null ? Mathf.CeilToInt(Def.HeaderIconSize.y) : 0;
    }

    public virtual int Compare(Def a, Def b)
    {
        return 0;
    }

    protected virtual Rect GetInteractableHeaderRect(Rect headerRect, DefTable defTable)
    {
        var height = Mathf.Min(25f, headerRect.height);
        return new Rect(headerRect.x, headerRect.yMax - height, headerRect.width, height);
    }

    protected virtual void HeaderClicked(Rect headerRect, DefTable defTable)
    {
        if (!Def.sortable || Event.current.shift)
            return;
        if (Event.current.button == 0)
        {
            if (defTable.SortingBy == null || !defTable.SortingBy.Equals(Def))
            {
                defTable.SortBy(Def, true);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            else if (defTable.SortingDescending)
            {
                defTable.SortBy(Def, false);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            else
            {
                defTable.SortBy(null, false);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
        }
        else
        {
            if (Event.current.button != 1)
                return;
            if (defTable.SortingBy == null || !defTable.SortingBy.Equals(Def))
            {
                defTable.SortBy(Def, false);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            else if (defTable.SortingDescending)
            {
                defTable.SortBy(null, false);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
            else
            {
                defTable.SortBy(Def, true);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }
    }

    protected virtual string GetHeaderTip(DefTable defTable)
    {
        var stringBuilder = new StringBuilder();
        if (!Def.headerTip.NullOrEmpty())
            stringBuilder.Append(Def.headerTip);
        if (Def.sortable)
        {
            if (stringBuilder.Length != 0)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine();
            }

            stringBuilder.Append("ClickToSortByThisColumn".Translate());
        }

        return stringBuilder.ToString();
    }
}