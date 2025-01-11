using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace PawnEditor;

public class UITable<T> : IComparer<UITable<T>.Row>, UIElement
{
    private static readonly float rowHeight = 32f;
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

    public bool Initialized => cachedRect != default && target != null;

    public int Compare(Row x, Row y) => sortIndex == -1 ? 0 : x.Items[sortIndex].SortIndex.CompareTo(y.Items[sortIndex].SortIndex) * sortDirection;
    public Vector2 Position => cachedRect.position;
    public float Width => cachedRect.width;
    public float Height => rows.NullOrEmpty() ? Text.LineHeightOf(GameFont.Small) : Heading.Height + 2f + rows.Count * (rowHeight + 2f);
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

    public void CheckRecache(Rect inRect, T target)
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
    }

    public void OnGUI(Rect inRect, T target)
    {
        CheckRecache(inRect, target);
        if (rows.Count == 0)
        {
            var rect = inRect;
            rect.xMin += 4f;
            Widgets.Label(rect, "None".Translate().Colorize(ColoredText.SubtleGrayColor));
            TooltipHandler.TipRegionByKey(inRect, "None");
            return;
        }

        var headerRect = inRect.TakeTopPart(Heading.Height);
        for (var i = 0; i < headings.Count; i++)
        {
            var currentColRect = headings[i].Draw(ref headerRect, i == 0, firstHasIcon, out var labelWidth, cachedWidths[i]);
            Widgets.DrawHighlightIfMouseover(currentColRect);
            if (!headings[i].Sortable) continue;
            if (sortIndex == i)
            {
                var texture2D = sortDirection == -1 ? PawnColumnWorker.SortingDescendingIcon : PawnColumnWorker.SortingIcon;
                GUI.DrawTexture(
                    new(currentColRect.xMin + labelWidth + texture2D.width - 6f, currentColRect.yMax - texture2D.height - 1f, texture2D.width,
                        texture2D.height), texture2D);
            }

            if (Widgets.ButtonInvisible(currentColRect))
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
        Widgets.DrawLineHorizontal(inRect.x + 4f, inRect.y + 1, inRect.width - 5f);
        GUI.color = Color.white;
        inRect.yMin += 2;

        for (var i = 0; i < rows.Count; i++)
        {
            var rect = inRect.TakeTopPart(rowHeight);
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
            private readonly TextAnchor textAnchor;
            private readonly Func<bool> shouldDraw;
            private readonly Color color = Color.white;

            public int SortIndex => sortIndex;

            public Item(string label, int sortIndex = -1, TextAnchor textAnchor = TextAnchor.MiddleCenter)
            {
                this.label = label;
                this.sortIndex = sortIndex;
                this.textAnchor = textAnchor;
            }

            public Item(string label, Color color, int sortIndex = -1, TextAnchor textAnchor = TextAnchor.MiddleCenter)
            {
                this.label = label;
                this.sortIndex = sortIndex;
                this.textAnchor = textAnchor;
                this.color = color;
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

            public Item(Texture icon, Action buttonClicked, Func<bool> shouldDraw = null)
            {
                this.icon = icon;
                this.buttonClicked = buttonClicked;
                this.shouldDraw = shouldDraw;
                sortIndex = -1;
            }

            public bool HasIcon => icon != null;

            public void Draw(Rect inRect)
            {
                inRect.x -= 4;
                if (shouldDraw != null && !shouldDraw()) return;
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
                        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(inRect, label.Truncate(inRect.width));
                        if (Widgets.ButtonInvisible(inRect)) buttonClicked();
                    }
                    else if (icon != null)
                    {
                        var scale = inRect.height / icon.height;
                        var rect = new Rect(0, 0, icon.width * scale, icon.height * scale)
                            .CenteredOnXIn(inRect)
                            .CenteredOnYIn(inRect)
                            .ContractedBy(4f);
                        GUI.color = Mouse.IsOver(inRect) ? GenUI.MouseoverColor : Color.white;
                        GUI.DrawTexture(rect, icon);
                        GUI.color = Color.white;
                        if (Widgets.ButtonInvisible(rect)) buttonClicked();
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
                       .ContractedBy(4f), icon);
                }
                else if (!label.NullOrEmpty())
                {
                    if (icon != null)
                    {
                        GUI.DrawTexture(inRect.TakeLeftPart(30).ContractedBy(2.5f), icon);
                        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(inRect, label);
                    }
                    else
                        using (new TextBlock(textAnchor))
                            Widgets.Label(inRect, label.Truncate(inRect.width).Colorize(color));
                }
            }
        }
    }

    public readonly struct Heading
    {
        private readonly string label;
        private readonly Texture2D icon;
        private readonly float scale;
        private readonly TextAnchor textAnchor;

        public static float Height => Text.LineHeightOf(GameFont.Small);

        public Heading() => Expandable = true;

        public Heading(float width) => Width = width;

        public Heading(string label, float? width = null, TextAnchor textAnchor = TextAnchor.LowerCenter)
        {
            this.label = label;
            Expandable = width == null;
            Width = width ?? Text.CalcSize(label).x;
            this.textAnchor = textAnchor;
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

        public Rect Draw(ref Rect outerRect, bool first, bool skipIcon, out float labelWidth, float? widthOverride = null)
        {
            var rect = outerRect.TakeLeftPart(widthOverride ?? Width);
            labelWidth = 0;
            var headerRect = rect;
            if (first) rect.TakeLeftPart(skipIcon ? 34 : 4);
            if (icon != null)
            {
                rect = new Rect(0, 0, icon.width * scale, icon.height * scale).CenteredOnXIn(rect).CenteredOnYIn(rect).ContractedBy(2.5f);
                GUI.DrawTexture(rect.ExpandedBy(2f), icon);
            }
            else if (!label.NullOrEmpty())
                using (new TextBlock(GameFont.Small))
                {
                    var newRect = new Rect(rect) { size = Text.CalcSize(label) };
                    newRect = newRect.ExpandedBy(2);
                    if (!first) newRect = newRect.CenteredOnXIn(rect);
                    newRect.y += rect.height - newRect.height;
                    labelWidth = Text.CalcSize(label).x;
                    if (textAnchor == TextAnchor.LowerCenter) labelWidth += (headerRect.width - labelWidth) / 2;

                    using (new TextBlock(first ? TextAnchor.LowerLeft : textAnchor))
                        Widgets.Label(rect, label);
                }

            return headerRect;
        }
    }
}

public interface UIElement
{
    Vector2 Position { get; }
    float Width { get; }
    float Height { get; }
}

public abstract class TabWorker_Table<T> : TabWorker<T>
{
    private static readonly Dictionary<Type, UITable<T>> allTables = new();
    protected UITable<T> table;

    public static void ClearCacheFor<T2>()
    {
        ClearCacheFor(typeof(T2));
    }

    public static void ClearCacheFor(Type type)
    {
        allTables[type].ClearCache();
    }

    protected abstract List<UITable<T>.Heading> GetHeadings();
    protected abstract List<UITable<T>.Row> GetRows(T target);

    public override void Initialize()
    {
        base.Initialize();
        table ??= new(GetHeadings(), GetRows);
        allTables.SetOrAdd(GetType(), table);
    }

    protected override void Notify_Open()
    {
        base.Notify_Open();
        table.ClearCache();
    }
}