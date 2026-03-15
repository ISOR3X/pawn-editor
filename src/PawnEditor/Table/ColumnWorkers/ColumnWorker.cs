using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;

[StaticConstructorOnStartup]
public abstract class ColumnWorker<T> where T : class
{
    protected const int DefaultCellHeight = 30;
    private static readonly Texture2D SortingIcon = ContentFinder<Texture2D>.Get("UI/Icons/Sorting");

    private static readonly Texture2D
        SortingDescendingIcon = ContentFinder<Texture2D>.Get("UI/Icons/SortingDescending");

    public required ColumnDef Def;
    protected virtual TextAnchor HeaderLabelAlignment => TextAnchor.LowerCenter;

    protected virtual Color HeaderColor => Color.white;
    protected virtual GameFont HeaderFont => GameFont.Small;

    public virtual bool VisibleCurrently => true;

    public virtual void DoHeader(Rect rect, TableWorker<T> table)
    {
        if (!Def.label.NullOrEmpty())
        {
            using (new TextBlock(HeaderFont, HeaderLabelAlignment, false))
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

        if (table.SortingBy != null && table.SortingBy == this)
        {
            var image = table.SortingDescending ? SortingDescendingIcon : SortingIcon;
            GUI.DrawTexture(
                new Rect((float)(rect.xMax - (double)image.width - 1.0),
                    (float)(rect.yMax - (double)image.height - 1.0), image.width, image.height), image);
        }

        if (!Def.HeaderInteractable)
            return;
        var interactableHeaderRect = GetInteractableHeaderRect(rect, table);
        if (Mouse.IsOver(interactableHeaderRect))
        {
            Verse.Widgets.DrawHighlight(interactableHeaderRect);
            var headerTip = GetHeaderTip(table);
            if (!headerTip.NullOrEmpty())
                TooltipHandler.TipRegion(interactableHeaderRect, (TipSignal)headerTip);
        }

        if (!Verse.Widgets.ButtonInvisible(interactableHeaderRect))
            return;
        HeaderClicked(rect, table);
    }

    public abstract void DoCell(Rect inRect, T thing, TableWorker<T> table);

    public virtual bool CanGroupWith(T thing, T other)
    {
        return false;
    }


    public virtual int GetMinCellHeight(T thing)
    {
        return (int)TableWorker<T>.DefaultRowHeight;
    }

    public virtual int GetMinHeaderHeight(TableWorker<T> table)
    {
        if (!Def.label.NullOrEmpty())
            // Use TextBlock to restore the previous font state rather than hardcoding Small.
            using (new TextBlock(HeaderFont))
            {
                return Mathf.CeilToInt(Text.CalcSize(Def.LabelCap).y);
            }

        return Def.HeaderIcon != null ? Mathf.CeilToInt(Def.HeaderIconSize.y) : 0;
    }

    public virtual int Compare(T a, T b)
    {
        return 0;
    }

    public float MeasureHeaderWidth()
    {
        var w = 0;
        if (!Def.label.NullOrEmpty())
            using (new TextBlock(HeaderFont))
                w += Mathf.CeilToInt(Text.CalcSize(Def.LabelCap).x);

        if (Def.HeaderIcon != null)
            w += Mathf.CeilToInt(Def.HeaderIconSize.x);

        return w + UIUtility.LabelPadding * 2;
    }

    protected virtual Rect GetInteractableHeaderRect(Rect headerRect, TableWorker<T> table)
    {
        var height = Mathf.Min(25f, headerRect.height);
        return new Rect(headerRect.x, headerRect.yMax - height, headerRect.width, height);
    }

    protected virtual void HeaderClicked(Rect headerRect, TableWorker<T> table)
    {
        if (!Def.sortable || Event.current.shift)
            return;
        if (Event.current.button == 0)
        {
            if (table.SortingBy == null || table.SortingBy != this)
            {
                table.SortBy(this, true);
            }
            else if (table.SortingDescending)
            {
                table.SortBy(this, false);
            }
            else
            {
                table.SortBy(null, false);
            }

            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }
        else
        {
            if (Event.current.button != 1)
                return;
            if (table.SortingBy == null || table.SortingBy != this)
            {
                table.SortBy(this, false);
            }
            else if (table.SortingDescending)
            {
                table.SortBy(null, false);
            }
            else
            {
                table.SortBy(this, true);
            }

            SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }
    }

    protected virtual string GetHeaderTip(TableWorker<T> table)
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

public abstract class DefColumnWorker : ColumnWorker<Def>;

public abstract class ThingColumnWorker : ColumnWorker<Thing>;