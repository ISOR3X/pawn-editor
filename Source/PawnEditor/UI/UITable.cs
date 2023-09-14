using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;

public class UITable<T> : IComparer<UITable<T>.Row>
{
    private readonly Func<T, IEnumerable<Row>> getRows;
    private readonly List<Heading> headings;
    private Rect cachedRect;
    private List<float> cachedWidths;
    private bool firstHasIcon;
    private List<Row> rows;
    private int sortDirection;
    private int sortIndex = -1;
    private T target;

    public UITable(List<Heading> headings, Func<T, IEnumerable<Row>> getRows)
    {
        this.headings = headings;
        this.getRows = getRows;
    }

    public int Compare(Row x, Row y) => sortIndex == -1 ? 0 : x.Items[sortIndex].SortIndex.CompareTo(y.Items[sortIndex].SortIndex) * sortDirection;

    private void RecacheRows()
    {
        rows = getRows(target).ToList();
        rows.Sort(this);
        firstHasIcon = rows.Any(row => row.Items.FirstOrDefault().HasIcon);
    }

    private void RecacheWidths()
    {
        cachedWidths = headings.Select(h => h.Width).ToList();
        var availWidth = cachedRect.width - cachedWidths.Sum();
        if (availWidth < 0)
            for (var i = 0; i < cachedWidths.Count; i++)
                cachedWidths[i] += availWidth / cachedWidths.Count;
        else if (availWidth > 0)
        {
            var expandables = headings.Count(h => h.Expandable);
            for (var i = 0; i < cachedWidths.Count; i++)
                if (headings[i].Expandable)
                    cachedWidths[i] += availWidth / expandables;
        }
    }

    public void ClearCache()
    {
        cachedRect = default;
        target = default;
    }

    public void OnGUI(Rect inRect, T target)
    {
        if (this.target?.GetHashCode() != target.GetHashCode())
        {
            this.target = target;
            RecacheRows();
        }

        if (cachedRect != inRect)
        {
            cachedRect = inRect;
            RecacheWidths();
        }

        if (rows.Count == 0)
        {
            Widgets.Label(inRect, "None".Translate().Colorize(ColoredText.SubtleGrayColor));
            TooltipHandler.TipRegionByKey(inRect, "None");
            return;
        }

        var headerRect = inRect.TakeTopPart(Heading.Height);
        for (var i = 0; i < headings.Count; i++)
        {
            var rect = headings[i].Draw(ref headerRect, i == 0, firstHasIcon, cachedWidths[i]);
            Widgets.DrawHighlightIfMouseover(rect);
            if (!headings[i].Sortable) continue;
            if (sortIndex == i)
            {
                var texture2D = sortDirection == -1 ? PawnColumnWorker.SortingDescendingIcon : PawnColumnWorker.SortingIcon;
                GUI.DrawTexture(new(rect.xMax - texture2D.width - 1f, rect.yMax - texture2D.height - 1f, texture2D.width, texture2D.height), texture2D);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                if (Event.current.button == 0)
                {
                    if (sortIndex != i)
                    {
                        sortIndex = i;
                        sortDirection = 1;
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }
                    else if (sortDirection == 1)
                    {
                        sortDirection = -1;
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }
                    else
                    {
                        sortIndex = -1;
                        sortDirection = 0;
                        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    }
                }
                else if (Event.current.button == 1)
                {
                    if (sortIndex != i)
                    {
                        sortIndex = i;
                        sortDirection = -1;
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }
                    else if (sortDirection == -1)
                    {
                        sortDirection = 1;
                        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    }
                    else
                    {
                        sortIndex = -1;
                        sortDirection = 0;
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }
                }

                rows.Sort(this);
            }
        }

        GUI.color = Widgets.SeparatorLineColor;
        Widgets.DrawLineHorizontal(inRect.x, inRect.y + 1, inRect.width);
        GUI.color = Color.white;
        inRect.yMin += 2;

        for (var i = 0; i < rows.Count; i++)
        {
            var rect = inRect.TakeTopPart(30);
            rect.xMin += 4;
            var totalRect = new Rect(rect);
            if (i % 2 == 1) Widgets.DrawLightHighlight(rect);
            var count = rows[i].Items.Count;
            for (var j = 0; j < count; j++) rows[i].Items[j].Draw(rect.TakeLeftPart(cachedWidths[j]));

            if (Mouse.IsOver(totalRect))
            {
                Widgets.DrawHighlight(totalRect);
                TooltipHandler.TipRegion(totalRect, rows[i].Tooltip);
            }
        }
    }

    public readonly struct Row
    {
        public List<Item> Items { get; }
        public string Tooltip { get; }

        public Row(IEnumerable<Item> items, string tooltip = null)
        {
            Items = items.ToList();
            Tooltip = tooltip;
        }

        public readonly struct Item
        {
            private readonly string label;
            private readonly Texture icon;
            private readonly int sortIndex;
            private readonly Action<Rect> customDrawer;
            private readonly Action buttonClicked;

            public int SortIndex => sortIndex;

            public Item(string label, int sortIndex = -1)
            {
                this.label = label;
                this.sortIndex = sortIndex;
            }

            public Item(string label, Action buttonClicked)
            {
                this.label = label;
                this.buttonClicked = buttonClicked;
                sortIndex = -1;
            }

            public Item(Action<Rect> drawer, int sortIndex = -1)
            {
                customDrawer = drawer;
                this.sortIndex = sortIndex;
            }

            public Item(string label, Texture icon, int sortIndex = -1)
            {
                this.label = label;
                this.icon = icon;
                this.sortIndex = sortIndex;
            }

            public Item(Texture icon, int sortIndex = -1)
            {
                this.icon = icon;
                this.sortIndex = sortIndex;
            }

            public Item(Texture icon, Action buttonClicked)
            {
                this.icon = icon;
                this.buttonClicked = buttonClicked;
                sortIndex = -1;
            }

            public bool HasIcon => icon != null;

            public void Draw(Rect inRect)
            {
                if (customDrawer != null) customDrawer(inRect);
                else if (buttonClicked != null)
                {
                    if (icon != null && !label.NullOrEmpty())
                    {
                        var atlas = Widgets.ButtonBGAtlas;
                        if (Mouse.IsOver(inRect))
                        {
                            atlas = Widgets.ButtonBGAtlasMouseover;
                            if (Input.GetMouseButton(0)) atlas = Widgets.ButtonBGAtlasClick;
                        }

                        Widgets.DrawAtlas(inRect, atlas);
                        GUI.DrawTexture(inRect.TakeLeftPart(30).ContractedBy(2.5f), icon);
                        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(inRect, label);
                        if (Widgets.ButtonInvisible(inRect)) buttonClicked();
                    }
                    else if (icon != null)
                    {
                        GUI.color = Mouse.IsOver(inRect) ? GenUI.MouseoverColor : Color.white;
                        GUI.DrawTexture(inRect, icon);
                        GUI.color = Color.white;
                        if (Widgets.ButtonInvisible(inRect)) buttonClicked();
                    }
                    else if (!label.NullOrEmpty())
                        if (Widgets.ButtonText(inRect, label))
                            buttonClicked();
                }
                else if (icon != null && label.NullOrEmpty())
                {
                    var scale = inRect.height / icon.height;
                    GUI.DrawTexture(new Rect(0, 0, icon.width * scale, icon.height * scale)
                       .CenteredOnXIn(inRect)
                       .CenteredOnYIn(inRect)
                       .ContractedBy(2.5f), icon);
                }
                else if (!label.NullOrEmpty())
                {
                    if (icon != null)
                    {
                        GUI.DrawTexture(inRect.TakeLeftPart(30).ContractedBy(2.5f), icon);
                        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(inRect, label);
                    }
                    else
                        using (new TextBlock(TextAnchor.MiddleCenter))
                            Widgets.Label(inRect, label);
                }
            }
        }
    }

    public readonly struct Heading
    {
        private readonly string label;
        private readonly Texture2D icon;
        private readonly float scale;

        public static float Height => Text.LineHeightOf(GameFont.Medium);

        public Heading() => Expandable = true;

        public Heading(float width) => Width = width;

        public Heading(string label, float? width = null)
        {
            this.label = label;
            Expandable = width == null;
            Width = width ?? Text.CalcSize(label).x;
        }

        public Heading(Texture2D icon, float? width = null)
        {
            this.icon = icon;
            Expandable = width == null;
            Width = width ?? icon.width;
            scale = Height / icon.height;
        }

        public bool Visible => !label.NullOrEmpty() || icon != null;
        public bool Sortable => Visible;
        public readonly float Width;
        public readonly bool Expandable;

        public Rect Draw(ref Rect outerRect, bool first, bool skipIcon, float? widthOverride = null)
        {
            var rect = outerRect.TakeLeftPart(widthOverride ?? Width);
            if (first) rect.TakeLeftPart(skipIcon ? 34 : 4);
            if (icon != null)
            {
                rect = new Rect(0, 0, icon.width * scale, icon.height * scale).CenteredOnXIn(rect).CenteredOnYIn(rect);
                if (Math.Abs(rect.height - outerRect.height) < 1f) rect = rect.ContractedBy(2.5f);
                GUI.DrawTexture(rect, icon);
                rect = rect.ExpandedBy(2);
            }
            else if (!label.NullOrEmpty())
                using (new TextBlock(GameFont.Small))
                {
                    var newRect = new Rect(rect) { size = Text.CalcSize(label) };
                    newRect = newRect.ExpandedBy(2);
                    if (!first) newRect = newRect.CenteredOnXIn(rect);
                    newRect.y += rect.height - newRect.height;
                    using (new TextBlock(first ? TextAnchor.LowerLeft : TextAnchor.LowerCenter))
                        Widgets.Label(rect, label);
                    rect = newRect;
                }

            return rect;
        }
    }
}

public abstract class TabWorker_Table<T> : TabWorker<T>
{
    protected UITable<T> table;
    protected abstract List<UITable<T>.Heading> GetHeadings();
    protected abstract List<UITable<T>.Row> GetRows(T target);

    public override void Initialize()
    {
        base.Initialize();
        table ??= new(GetHeadings(), GetRows);
    }
}
