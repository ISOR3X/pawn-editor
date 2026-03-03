using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;

[StaticConstructorOnStartup]
public abstract class ColumnWorker
{
    public ColumnDef def;
    protected const int DefaultCellHeight = 30;
    private static readonly Texture2D SortingIcon = ContentFinder<Texture2D>.Get("UI/Icons/Sorting");
    private static readonly Texture2D SortingDescendingIcon = ContentFinder<Texture2D>.Get("UI/Icons/SortingDescending");
    protected virtual TextAnchor LabelAlignment => TextAnchor.LowerCenter;

    protected virtual Color HeaderColor => Color.white;
    protected virtual GameFont HeaderFont => GameFont.Small;

    public virtual bool VisibleCurrently => true;

    public virtual void DoHeader(Rect rect, DefTable defTable)
    {
        if (!def.label.NullOrEmpty())
        {
            using (new TextBlock(HeaderFont, LabelAlignment, false))
            {
                Rect rect1 = rect;
                rect1.y += 3f;
                Widgets.Label(rect1, def.LabelCap.Resolve().Truncate(rect.width).Colorize(HeaderColor));
            }
        }
        else if (def.HeaderIcon != null)
        {
            Vector2 headerIconSize = def.HeaderIconSize;
            int num = (int)((rect.width - (double)headerIconSize.x) / 2.0);
            GUI.DrawTexture(new Rect(rect.x + num, rect.yMax - headerIconSize.y, headerIconSize.x, headerIconSize.y).ContractedBy(2f), def.HeaderIcon);
        }

        if (defTable.SortingBy != null && defTable.SortingBy.Equals(def))
        {
            Texture2D image = defTable.SortingDescending ? SortingDescendingIcon : SortingIcon;
            GUI.DrawTexture(new Rect((float)(rect.xMax - (double)image.width - 1.0), (float)(rect.yMax - (double)image.height - 1.0), image.width, image.height), image);
        }

        if (!def.HeaderInteractable)
            return;
        Rect interactableHeaderRect = GetInteractableHeaderRect(rect, defTable);
        if (Mouse.IsOver(interactableHeaderRect))
        {
            Widgets.DrawHighlight(interactableHeaderRect);
            string headerTip = GetHeaderTip(defTable);
            if (!headerTip.NullOrEmpty())
                TooltipHandler.TipRegion(interactableHeaderRect, (TipSignal)headerTip);
        }

        if (!Widgets.ButtonInvisible(interactableHeaderRect))
            return;
        HeaderClicked(rect, defTable);
    }

    public abstract void DoCell(Rect inRect, Def thing, DefTable defTable);

    public virtual bool CanGroupWith(Def thing, Def other) => false;

    public virtual int GetMinWidth(DefTable defTable)
    {
        if (!def.label.NullOrEmpty())
        {
            Text.Font = HeaderFont;
            int minWidth = Mathf.CeilToInt(Text.CalcSize(def.LabelCap).x);
            Text.Font = GameFont.Small;
            return minWidth;
        }

        return def.HeaderIcon != null ? Mathf.CeilToInt(def.HeaderIconSize.x) : 1;
    }

    public virtual int GetMaxWidth(DefTable defTable) => 1000000;

    public virtual int GetOptimalWidth(DefTable defTable) => GetMinWidth(defTable);

    public virtual int GetMinCellHeight(Def thing) => (int)DefTable.defaultRowHeight;

    public virtual int GetMinHeaderHeight(DefTable defTable)
    {
        if (!def.label.NullOrEmpty())
        {
            Text.Font = HeaderFont;
            int minHeaderHeight = Mathf.CeilToInt(Text.CalcSize(def.LabelCap).y);
            Text.Font = GameFont.Small;
            return minHeaderHeight;
        }

        return def.HeaderIcon != null ? Mathf.CeilToInt(def.HeaderIconSize.y) : 0;
    }

    public virtual int Compare(Def a, Def b) => 0;

    protected virtual Rect GetInteractableHeaderRect(Rect headerRect, DefTable defTable)
    {
        float height = Mathf.Min(25f, headerRect.height);
        return new Rect(headerRect.x, headerRect.yMax - height, headerRect.width, height);
    }

    protected virtual void HeaderClicked(Rect headerRect, DefTable defTable)
    {
        if (!def.sortable || Event.current.shift)
            return;
        if (Event.current.button == 0)
        {
            if (defTable.SortingBy == null || !defTable.SortingBy.Equals(def))
            {
                defTable.SortBy(def, true);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            else if (defTable.SortingDescending)
            {
                defTable.SortBy(def, false);
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
            if (defTable.SortingBy == null || !defTable.SortingBy.Equals(def))
            {
                defTable.SortBy(def, false);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            else if (defTable.SortingDescending)
            {
                defTable.SortBy(null, false);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
            else
            {
                defTable.SortBy(def, true);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }
    }

    protected virtual string GetHeaderTip(DefTable defTable)
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (!def.headerTip.NullOrEmpty())
            stringBuilder.Append(def.headerTip);
        if (def.sortable)
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