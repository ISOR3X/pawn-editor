using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class ListingMenu<T> : Window
{
    protected readonly Pawn Pawn;
    private readonly Func<T, AddResult> _action;
    private readonly bool _allowMultiSelect;
    private readonly Action _closeAction;
    private readonly string _closeLabel;
    private readonly string _menuTitle;
    private readonly int _minCount;
    private readonly Func<List<T>, AddResult> _multiAction;

    protected Listing_Thing<T> Listing;
    protected static Dictionary<string, List<Filter<T>>> cachedActiveFilters = new();
    protected TreeNode_ThingCategory TreeNodeThingCategory;

    private Vector2 _scrollPosition;
    private bool _showFilters;
    private float _viewHeight = 100f;

    public ListingMenu(List<T> items, Func<T, string> labelGetter, Func<T, AddResult> action, string menuTitle,
        Func<T, string> descGetter = null, Action<T, Rect> iconDrawer = null, List<Filter<T>> filters = null, Pawn pawn = null, string nextLabel = null,
        string closeLabel = null, Action closeAction = null,
        IEnumerable<T> auxHighlight = null) :
        this(menuTitle, pawn, nextLabel, closeLabel, closeAction)
    {
        Listing = new(items.OrderBy(labelGetter).ToList(), labelGetter, iconDrawer, descGetter, filters, auxHighlight);
        _action = action;
        _allowMultiSelect = false;
    }

    public ListingMenu(List<T> items, Func<T, string> labelGetter, Func<List<T>, AddResult> action, string menuTitle, IntRange wantedCount,
        Func<T, string> descGetter = null, Action<T, Rect> iconDrawer = null, List<Filter<T>> filters = null, Pawn pawn = null, string nextLabel = null,
        string closeLabel = null, Action closeAction = null,
        IEnumerable<T> auxHighlight = null) :
        this(menuTitle, pawn, nextLabel, closeLabel, closeAction)
    {
        Listing = new(items.OrderBy(labelGetter).ToList(), wantedCount.TrueMax, labelGetter, iconDrawer, descGetter, filters, auxHighlight);
        _multiAction = action;
        _allowMultiSelect = true;
        _minCount = wantedCount.TrueMin;
    }

    protected ListingMenu(string menuTitle, Pawn pawn = null, string nextLabel = null, string closeLabel = null, Action closeAction = null) : this(menuTitle,
        pawn)
    {
        NextLabel = nextLabel.NullOrEmpty() ? "Add".Translate().CapitalizeFirst() : nextLabel;
        _closeLabel = closeLabel.NullOrEmpty() ? "Close".Translate() : closeLabel;
        _closeAction = closeAction;
    }

    protected ListingMenu(Func<T, AddResult> action, string menuTitle, Pawn pawn = null) : this(menuTitle, pawn)
    {
        _action = action;
        _allowMultiSelect = false;
        NextLabel = "Add".Translate().CapitalizeFirst();
        _closeLabel = "Close".Translate();
    }

    protected ListingMenu(string menuTitle, Pawn pawn = null)
    {
        Pawn = pawn;
        _menuTitle = menuTitle;

        draggable = true;
        closeOnClickedOutside = true;
        onlyOneOfTypeAllowed = true;
    }

    public override void PreOpen()
    {
        base.PreOpen();
        Listing.ActiveFilters = cachedActiveFilters.TryGetValue(_menuTitle, fallback: new());
    }

    public override void PostClose()
    {
        base.PostClose();
        cachedActiveFilters[_menuTitle] = Listing.ActiveFilters;
    }

    protected virtual string NextLabel { get; }

    public override Vector2 InitialSize => new(400f, 600f);
    private static Vector2 WideSize => new(800f, 600f);


    public override void DoWindowContents(Rect inRect)
    {
        DrawHeader(inRect.TakeTopPart(Text.LineHeightOf(GameFont.Medium)));
        inRect.yMin += 16f;

        var leftRect = inRect.TakeLeftPart(InitialSize.x - StandardMargin * 2);
        var bottomButRect = leftRect.TakeBottomPart(UIUtility.BottomButtonSize.y);
        DrawBottomButtons(bottomButRect);
        DrawFooter(ref leftRect);
        DrawFootnote(leftRect.TakeBottomPart(Text.LineHeightOf(GameFont.Small) + 8f));


        DrawListing(leftRect);

        if (Listing.Filters != null && _showFilters)
        {
            inRect.xMin += 16f;
            DrawFilters(inRect);
        }

        UpdateWindowRect();
        CloseIfNotSelected();
    }

    protected virtual void DrawFooter(ref Rect inRect)
    {
    }

    private void DrawHeader(Rect inRect)
    {
        using (new TextBlock(GameFont.Medium, TextAnchor.MiddleLeft, false)) Widgets.Label(inRect.TakeLeftPart(Text.CalcSize(_menuTitle).x), _menuTitle);

        if (Pawn == null) return;
        using (new TextBlock(GameFont.Medium))
        {
            var lineHeight = Text.LineHeight;
            var name = Pawn.Name.ToStringShort.Colorize(Color.white);
            float scaleFactor = 1;

            if (!Pawn.NonHumanlikeOrWildMan())
            {
                var job = (", " + Pawn.story.TitleCap).Colorize(ColoredText.SubtleGrayColor);
                scaleFactor = 8f;

                if (inRect.width >= Text.CalcSize(name + job).x + lineHeight)
                    name += job;
            }

            using (new TextBlock(TextAnchor.MiddleRight))
                Widgets.Label(inRect.TakeRightPart(Text.CalcSize(name).x + 8f), name);

            var portraitRect = inRect.TakeRightPart(inRect.height);
            portraitRect.height = inRect.height;

            portraitRect = portraitRect.ExpandedBy(1 * scaleFactor);
            Widgets.ThingIcon(portraitRect, Pawn);
        }
    }

    private void DrawListing(Rect inRect)
    {
        Widgets.DrawMenuSection(inRect);
        if (TreeNodeThingCategory != null)
            using (new TextBlock(GameFont.Tiny))
                DrawNodeCollapse(inRect.TakeTopPart(26f).ContractedBy(1f));

        inRect = inRect.ContractedBy(4f);
        inRect.yMin -= 4f;
        var viewRect = new Rect(0.0f, 0.0f, inRect.width - 16f, _viewHeight);
        var visibleRect = new Rect(0.0f, 0.0f, inRect.width, inRect.height);
        visibleRect.position += _scrollPosition;
        var outRect = inRect;
        outRect.yMax -= UIUtility.SearchBarHeight + 4f;
        Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);
        var rect3 = new Rect(0.0f, 2f, viewRect.width, 999999f);
        visibleRect.position -= rect3.position;
        Listing.Begin(rect3);
        if (Listing is Listing_TreeThing listingTree)
            listingTree.ListCategoryChildren(TreeNodeThingCategory, 1, visibleRect);
        else
            Listing.ListChildren(visibleRect);

        Listing.End();
        if (Event.current.type == EventType.Layout)
            _viewHeight = Listing.CurHeight;
        Widgets.EndScrollView();

        Listing.DrawSearchBar(new(inRect.x, inRect.yMax - UIUtility.SearchBarHeight, inRect.width,
            UIUtility.SearchBarHeight));
    }

    private void DrawFootnote(Rect inRect)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            DrawSelected(ref inRect);
            // Show filters checkbox
            if (Listing.Filters == null) return;
            var checkboxLabelRect = inRect.TakeRightPart(Widgets.CheckboxSize + 8f);
            Widgets.Checkbox(new(checkboxLabelRect.xMax - Widgets.CheckboxSize, checkboxLabelRect.yMin + (checkboxLabelRect.height - Widgets.CheckboxSize) / 2),
                ref _showFilters);
            GUI.color = ColoredText.SubtleGrayColor;
            using (new TextBlock(TextAnchor.MiddleRight))
                Widgets.Label(inRect, "Show filters");
            GUI.color = Color.white;
        }
    }

    protected virtual void DrawSelected(ref Rect inRect)
    {
        // Current selection label
        if (Listing.IconDrawer != null)
        {
            if (_allowMultiSelect)
                foreach (var item in Listing.MultiSelected)
                {
                    Listing.IconDrawer(item, new(inRect.x, inRect.y, 32f, 32f));
                    inRect.xMin += 32f;
                }
            else
            {
                Listing.IconDrawer(Listing.Selected, new(inRect.x, inRect.y, 32f, 32f));
                inRect.xMin += 32f;
            }
        }

        var labelStr = $"{"StartingPawnsSelected".Translate()}: ";
        var labelWidth = Text.CalcSize(labelStr).x;
        var selectedStr = _allowMultiSelect ? Listing.MultiSelected.Count == 0 ? "None".Translate() : Listing.MultiSelected.Join(Listing.LabelGetter) :
            Listing.Selected != null ? (TaggedString)Listing.LabelGetter(Listing.Selected) : "None".Translate();
        Widgets.Label(inRect, labelStr.Colorize(ColoredText.SubtleGrayColor));
        inRect.xMin += labelWidth;
        Widgets.Label(inRect, selectedStr);
    }

    private void DrawBottomButtons(Rect inRect)
    {
        if (_allowMultiSelect ? Listing.MultiSelected.Count >= _minCount : Listing.Selected != null)
        {
            if (Widgets.ButtonText(new(inRect.xMax - UIUtility.BottomButtonSize.x, inRect.y, UIUtility.BottomButtonSize.x, UIUtility.BottomButtonSize.y),
                    NextLabel))
                (_allowMultiSelect ? _multiAction(Listing.MultiSelected) : _action(Listing.Selected)).HandleResult(() => Close());

            if (Widgets.ButtonText(new(inRect.x, inRect.y, UIUtility.BottomButtonSize.x, UIUtility.BottomButtonSize.y), _closeLabel))
            {
                _closeAction?.Invoke();
                Close();
            }
        }
        else
        {
            if (Widgets.ButtonText(
                    new((float)((inRect.width - (double)UIUtility.BottomButtonSize.x) / 2.0), inRect.y, UIUtility.BottomButtonSize.x,
                        UIUtility.BottomButtonSize.y), _closeLabel))
            {
                _closeAction?.Invoke();
                Close();
            }
        }
    }

    private void DrawFilters(Rect inRect)
    {
        var allFilters = Listing.Filters;
        var activeFilters = Listing.ActiveFilters;

        UIUtility.ListSeparator(inRect.TakeTopPart(Text.LineHeightOf(GameFont.Small) + 8f), $"{"PawnEditor.Filters".Translate().CapitalizeFirst()}");
        string label1 = "Add".Translate().CapitalizeFirst() + " " + "PawnEditor.Filter".Translate().ToLower() + "...";
        string label2 = "RemoveOrgan".Translate().CapitalizeFirst() + " " + "PawnEditor.All".Translate().ToLower();
        var buttonRect = inRect.TakeTopPart(UIUtility.RegularButtonHeight);

        var list = new List<FloatMenuOption>();

        foreach (var filter in allFilters)
            list.Insert(0, new(filter.Label, delegate
            {
                var maxFilterCount = allFilters.Count(f => f.Label == filter.Label);

                if (activeFilters.Count(f => f.Label == filter.Label) < maxFilterCount)
                    activeFilters.Add(allFilters.FirstOrDefault(f => f.Label == filter.Label && !activeFilters.Contains(f)));
                else
                    Messages.Message(new("Reached limit of this specific filter count", MessageTypeDefOf.RejectInput));
            }));

        var distinctList = list.GroupBy(l => l.Label).Select(m => m.First()).OrderBy(n => n.Label).ToList();

        if (Widgets.ButtonText(buttonRect.TakeLeftPart(inRect.width * 0.7f), label1)) Find.WindowStack.Add(new FloatMenu(distinctList));

        buttonRect.xMin += 4f;
        if (Widgets.ButtonText(buttonRect, label2))
        {
            activeFilters.ForEach(f => f.Inverted = false);
            activeFilters.Clear();
        }

        inRect.yMin += 4f;

        var filtersToRemove = new List<Filter<T>>();
        foreach (var activeFilter in activeFilters)
        {
            if (activeFilter.DrawFilter(ref inRect))
                filtersToRemove.Add(activeFilter);
            inRect.yMin += 4f;
        }


        filtersToRemove.ForEach(ftr => activeFilters.Remove(ftr));
    }

    private void DrawNodeCollapse(Rect inRect)
    {
        if (Widgets.ButtonText(inRect.RightHalf(), "OpenFolder".Translate() + " " + "AllDays".Translate()))
            foreach (var treeNodeThingCategory in TreeNodeThingCategory.ChildCategoryNodes)
            {
                treeNodeThingCategory.SetOpen(1, true);
                foreach (var child in treeNodeThingCategory.ChildCategoryNodes) child.SetOpen(1, true);
            }

        if (Widgets.ButtonText(inRect.LeftHalf(), "Close".Translate() + " " + "AllDays".Translate()))
            foreach (var treeNodeThingCategory in TreeNodeThingCategory.ChildCategoryNodes)
            {
                treeNodeThingCategory.SetOpen(1, false);
                foreach (var child in treeNodeThingCategory.ChildCategoryNodes) child.SetOpen(1, false);
            }
    }

    private void UpdateWindowRect()
    {
        if (_showFilters && windowRect.width != WideSize.x)
        {
            windowRect.width = WideSize.x;
            windowRect.height = WideSize.y;
        }
        else if (!_showFilters && windowRect.width != InitialSize.x)
        {
            windowRect.width = InitialSize.x;
            windowRect.height = InitialSize.y;
        }
    }

    private void CloseIfNotSelected()
    {
        if (Find.WindowStack.focusedWindow is Dialog_PawnEditor) Find.WindowStack.TryRemove(this);
    }
}