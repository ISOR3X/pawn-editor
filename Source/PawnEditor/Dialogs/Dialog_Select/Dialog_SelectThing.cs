using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

// ReSharper disable once InconsistentNaming
public abstract class Dialog_SelectThing<T> : Window
{
    protected abstract string PageTitle { get; }
    private float _viewHeight = 100f;
    public override Vector2 InitialSize => new(700, 700);
    protected bool HasOptions = false;

    private Vector2 _scrollPosition;
    protected Dictionary<string, string> truncateCache = new();
    protected TreeNode_ThingCategory TreeNodeThingCategory;
    protected readonly QuickSearchWidget _quickSearchWidget = new();

    private readonly List<TFilter<T>> _activeFilters = new();
    private List<TFilter<T>> _allFilters = new();
    protected Listing_Thing<T> Listing;
    protected Action<T> OnSelected;

    protected readonly List<T> ThingList;

    protected readonly Pawn CurPawn;

    private Dialog_SelectThing(List<T> thingList)
    {
        ThingList = thingList;

        Listing = new Listing_Thing<T>(_quickSearchWidget.filter, thingList);
    }

    protected Dialog_SelectThing(List<T> thingList, Pawn curPawn) : this(thingList)
    {
        CurPawn = curPawn;
    }

    protected virtual List<TFilter<T>> Filters()
    {
        List<TFilter<T>> filters = new List<TFilter<T>>();

        // Common filters
        if (typeof(Def).IsAssignableFrom(typeof(T)))
        {
            var defList = ThingList.OfType<Def>();
            var modSourceDict = new Dictionary<FloatMenuOption, Func<T, bool>>();
            LoadedModManager.runningMods
                .Where(m => m.AllDefs.Any(td => !td.generated && defList.Contains(td))).ToList()
                .ForEach(m =>
                {
                    string label = m.Name;
                    FloatMenuOption option = new FloatMenuOption(label, () => { });
                    modSourceDict.Add(option, type => ((Def)(object)type).modContentPack.Name == m.Name);
                });
            filters.Add(new TFilter<T>("Source".Translate(), false, modSourceDict, "PawnEditor.SourceDesc".Translate()));
        }

        return filters;
    }

    public override void PreOpen()
    {
        base.PreOpen();
        Utilities.SubMenuOpen = true;
        _allFilters = Filters();
        _quickSearchWidget.Reset();
        _activeFilters.AddRange(_allFilters.Where(f => f.EnabledByDefault));
    }

    public override void PostClose()
    {
        base.PostClose();
        Utilities.SubMenuOpen = false;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect rect = inRect;
        DrawHeader(rect.TakeTopPart(Text.LineHeightOf(GameFont.Medium)));
        rect.yMin += 16f;

        Rect bottomButRect = rect.TakeBottomPart(UIUtility.BottomButtonSize.y + 2 * 8f);
        DoBottomButtons(bottomButRect);

        Rect leftRect = rect.TakeLeftPart(280f);
        DrawScrollable(leftRect);

        rect.xMin += 16f;
        // Passed by reference so they grow vertically depending on the size of each.
        DrawInfoCard(ref rect);
        DrawOptions(ref rect);
        DrawFilters(ref rect);
    }

    private void DrawHeader(Rect rect)
    {
        DrawHeaderTitle(rect);
        if (CurPawn != null)
        {
            using (new TextBlock(GameFont.Medium))
            {
                float lineHeight = Text.LineHeight;
                Vector2 portraitSize = new(lineHeight, lineHeight);
                string name = CurPawn.Name.ToStringShort.Colorize(Color.white) +
                              (", " + CurPawn.story.TitleCap).Colorize(ColoredText.SubtleGrayColor);

                Rect portraitRect = rect;
                portraitRect.xMin = portraitRect.xMax - Text.CalcSize(name).x - portraitSize.x - 4f;
                (portraitRect.width, portraitRect.height) = (lineHeight, lineHeight);

                portraitRect = portraitRect.ExpandedBy(4f);
                Widgets.ThingIcon(portraitRect, CurPawn);

                using (new TextBlock(TextAnchor.MiddleRight))
                    Widgets.Label(rect, name);
            }
        }
    }

    private void DrawHeaderTitle(Rect rect)
    {
        Text.Font = GameFont.Medium;
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(rect, PageTitle);
        Text.Font = GameFont.Small;
    }

    private void DrawScrollable(Rect inRect)
    {
        // Selection button
        string addSelectedStr = $"{"Add".Translate().CapitalizeFirst()} {"PawnEditor.Selected".Translate()}";
        if (Widgets.ButtonText(inRect.TakeBottomPart(30f), addSelectedStr))
        {
            if (Listing.SelectedThing != null)
                OnSelected(Listing.SelectedThing);
        }

        // Current selection label
        Rect selectRect = inRect.TakeBottomPart(Text.LineHeightOf(GameFont.Small) + 8f);
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            if (Listing.showIcon)
            {
                Listing_Thing<T>.DrawThingIcon(new Rect(selectRect.x, selectRect.y, 32f, 32f), Listing.SelectedThing);
                selectRect.xMin += 32f;
            }

            string labelStr = $"{"StartingPawnsSelected".Translate()}: ";
            string selectedStr = Listing.SelectedThing != null ? Listing_Thing<T>.GetThingLabel(Listing.SelectedThing) : "None";
            float labelWidth = Text.CalcSize(labelStr).x;
            Widgets.Label(selectRect, labelStr.Colorize(ColoredText.SubtleGrayColor));
            selectRect.xMin += labelWidth;
            Widgets.Label(selectRect, selectedStr);
        }

        Widgets.DrawMenuSection(inRect);
        inRect = inRect.ContractedBy(4f);
        Rect viewRect = new Rect(0.0f, 0.0f, inRect.width - 16f, _viewHeight);
        Rect visibleRect = new Rect(0.0f, 0.0f, inRect.width, inRect.height);
        visibleRect.position += _scrollPosition;
        Rect outRect = inRect;
        outRect.yMax -= (UIUtility.SearchBarHeight + 4f);
        Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);
        Rect rect3 = new Rect(0.0f, 2f, viewRect.width, 999999f);
        visibleRect.position -= rect3.position;
        Listing.FilteredThings = _activeFilters.Any() ? ThingList.Where(td => _activeFilters.All(lf => lf.FilterAction(td))).ToList() : ThingList;
        Listing.Begin(rect3);
        if (Listing is Listing_TreeThing listingTree)
        {
            listingTree.ListCategoryChildren(TreeNodeThingCategory, 1, visibleRect);
        }
        else
        {
            Listing.ListChildren(visibleRect);
        }

        Listing.End();
        if (Event.current.type == EventType.Layout)
            _viewHeight = Listing.CurHeight;
        Widgets.EndScrollView();

        _quickSearchWidget.OnGUI(new Rect(inRect.x, inRect.yMax - UIUtility.SearchBarHeight, inRect.width,
            UIUtility.SearchBarHeight));
    }

    protected virtual void DrawInfoCard(ref Rect inRect)
    {
        UIUtility.ListSeparator(inRect.TakeTopPart(Text.LineHeightOf(GameFont.Small) + 8f), $"{"PawnEditor.Selected".Translate().CapitalizeFirst()}");
    }

    protected virtual void DrawOptions(ref Rect inRect)
    {
        if (HasOptions)
        {
            UIUtility.ListSeparator(inRect.TakeTopPart(Text.LineHeightOf(GameFont.Small) + 8f), $"{"Options".Translate().CapitalizeFirst()}");
        }
    }

    private void DrawFilters(ref Rect inRect)
    {
        var allFilters = _allFilters;
        if (allFilters.Any())
        {
            UIUtility.ListSeparator(inRect.TakeTopPart(Text.LineHeightOf(GameFont.Small) + 8f), $"{"PawnEditor.Filters".Translate().CapitalizeFirst()}");
            string label1 = "Add".Translate().CapitalizeFirst() + " " + "PawnEditor.Filter".Translate().ToLower() + "...";
            string label2 = "RemoveOrgan".Translate().CapitalizeFirst() + " " + "All".Translate().ToLower();
            Rect buttonRect = inRect.TakeTopPart(UIUtility.RegularButtonHeight);

            var list = new List<FloatMenuOption>();
            foreach (TFilter<T> filter in allFilters)
            {
                list.Insert(0, new FloatMenuOption(filter.Label, delegate
                {
                    int maxFilterCount = _allFilters.Count(f => f.Label == filter.Label);

                    if (_activeFilters.Count(f => f.Label == filter.Label) < maxFilterCount)
                    {
                        _activeFilters.Add(_allFilters.FirstOrDefault(f => f.Label == filter.Label && !_activeFilters.Contains(f)));
                    }
                    else
                    {
                        Messages.Message(new Message("Reached limit of this specific filter count", MessageTypeDefOf.RejectInput));
                    }
                }));
            }

            var distinctList = list.GroupBy(l => l.Label).Select(m => m.First()).OrderBy(n => n.Label).ToList();

            if (Widgets.ButtonText(buttonRect.TakeLeftPart(inRect.width * 0.7f), label1))
            {
                Find.WindowStack.Add(new FloatMenu(distinctList));
            }

            buttonRect.xMin += 4f;
            if (Widgets.ButtonText(buttonRect, label2))
            {
                _activeFilters.ForEach(f => f.DoInvert = false);
                _activeFilters.Clear();
            }

            inRect.yMin += 4f;
            float filterWidth = inRect.width;
            if (InitialSize.x > 800)
                filterWidth = filterWidth / 2f - 2f;

            List<TFilter<T>> filtersToRemove = new List<TFilter<T>>();
            Rect activeFiltersRect = GenUI.DrawElementStack(inRect, UIUtility.RegularButtonHeight, _activeFilters,
                delegate(Rect r, TFilter<T> thing) { thing.DrawFilter(r, ref filtersToRemove); }, _ => filterWidth, 4f, 4f, false);


            filtersToRemove.ForEach(ftr => _activeFilters.Remove(ftr));
            inRect.yMin += activeFiltersRect.height + 8f;
        }
    }

    private void DoBottomButtons(Rect inRect)
    {
        if (!Widgets.ButtonText(
                new Rect((float)((inRect.width - (double)UIUtility.BottomButtonSize.x) / 2.0), inRect.y + 8f,
                    UIUtility.BottomButtonSize.x, UIUtility.BottomButtonSize.y),
                "Close".Translate()))
            return;
        Close();
    }
}