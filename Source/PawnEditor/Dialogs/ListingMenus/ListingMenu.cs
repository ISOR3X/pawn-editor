using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class ListingMenu<T> : Window
{
    private readonly string _menuTitle;
    private bool _showFilters;
    private readonly Action<T> _action;

    protected readonly Pawn Pawn;

    protected Listing_Thing<T> Listing;
    protected TreeNode_ThingCategory TreeNodeThingCategory;

    private Vector2 _scrollPosition;
    private float _viewHeight = 100f;

    public override Vector2 InitialSize => new(400f, 600f);
    private static Vector2 WideSize => new(800f, 600f);

    public ListingMenu(List<T> items, Func<T, string> labelGetter, Action<T> action, string menuTitle,
        Func<T, string> descGetter = null, Action<T, Rect> iconDrawer = null, List<TFilter<T>> filters = null, Pawn pawn = null) : this(action, menuTitle, pawn)
    {
        Listing = new Listing_Thing<T>(items, labelGetter, iconDrawer, descGetter, filters);
    }

    protected ListingMenu(Action<T> action, string menuTitle, Pawn pawn = null)
    {
        Pawn = pawn;
        _action = action;
        _menuTitle = menuTitle;

        draggable = true;
        closeOnClickedOutside = true;
        onlyOneOfTypeAllowed = true;
    }


    public override void DoWindowContents(Rect inRect)
    {
        DrawHeader(inRect.TakeTopPart(Text.LineHeightOf(GameFont.Medium)));
        inRect.yMin += 16f;

        Rect leftRect = inRect.TakeLeftPart(InitialSize.x - StandardMargin * 2);
        Rect bottomButRect = leftRect.TakeBottomPart(UIUtility.BottomButtonSize.y);
        DrawBottomButtons(bottomButRect);

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

    private void DrawHeader(Rect inRect)
    {
        using (new TextBlock(GameFont.Medium, TextAnchor.MiddleLeft, false))
        {
            Widgets.Label(inRect.TakeLeftPart(Text.CalcSize(_menuTitle).x), _menuTitle);
        }
        
        if (Pawn == null) return;
        using (new TextBlock(GameFont.Medium))
        {
            float lineHeight = Text.LineHeight;
            string name = Pawn.Name.ToStringShort.Colorize(Color.white);
            float scaleFactor = 1;
            
            if (!Pawn.NonHumanlikeOrWildMan())
            {
                string job = (", " + Pawn.story.TitleCap).Colorize(ColoredText.SubtleGrayColor);
                scaleFactor = 8f;

                if (inRect.width >= Text.CalcSize(name + job).x + lineHeight)
                    name += job;
            }

            using (new TextBlock(TextAnchor.MiddleRight))
                Widgets.Label(inRect.TakeRightPart(Text.CalcSize(name).x + 8f), name);
            
            Rect portraitRect = inRect.TakeRightPart(inRect.height);
            portraitRect.height = inRect.height;

            portraitRect = portraitRect.ExpandedBy(1 * scaleFactor);
            Widgets.ThingIcon(portraitRect, Pawn);
        }
    }

    private void DrawListing(Rect inRect)
    {
        Widgets.DrawMenuSection(inRect);
        if (TreeNodeThingCategory != null)
        {
            using (new TextBlock(GameFont.Tiny))
                DrawNodeCollapse(inRect.TakeTopPart(26f).ContractedBy(1f));
        }

        inRect = inRect.ContractedBy(4f);
        inRect.yMin -= 4f;
        Rect viewRect = new Rect(0.0f, 0.0f, inRect.width - 16f, _viewHeight);
        Rect visibleRect = new Rect(0.0f, 0.0f, inRect.width, inRect.height);
        visibleRect.position += _scrollPosition;
        Rect outRect = inRect;
        outRect.yMax -= (UIUtility.SearchBarHeight + 4f);
        Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);
        Rect rect3 = new Rect(0.0f, 2f, viewRect.width, 999999f);
        visibleRect.position -= rect3.position;
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

        Listing.DrawSearchBar(new Rect(inRect.x, inRect.yMax - UIUtility.SearchBarHeight, inRect.width,
            UIUtility.SearchBarHeight));
    }

    private void DrawFootnote(Rect inRect)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            // Current selection label
            if (Listing.IconDrawer != null)
            {
                Listing.IconDrawer.Invoke(Listing.Selected, new Rect(inRect.x, inRect.y, 32f, 32f));
                inRect.xMin += 32f;
            }

            string labelStr = $"{"StartingPawnsSelected".Translate()}: ";
            float labelWidth = Text.CalcSize(labelStr).x;
            string selectedStr = Listing.Selected != null ? Listing.LabelGetter(Listing.Selected) : "None";
            Widgets.Label(inRect, labelStr.Colorize(ColoredText.SubtleGrayColor));
            inRect.xMin += labelWidth;
            Widgets.Label(inRect, selectedStr);

            // Show filters checkbox
            if (Listing.Filters == null) return;
            Rect checkboxLabelRect = inRect.TakeRightPart(Widgets.CheckboxSize + 8f);
            Widgets.Checkbox(new Vector2(checkboxLabelRect.xMax - Widgets.CheckboxSize, checkboxLabelRect.yMin + (checkboxLabelRect.height - Widgets.CheckboxSize) / 2), ref _showFilters);
            GUI.color = ColoredText.SubtleGrayColor;
            using (new TextBlock(TextAnchor.MiddleRight))
                Widgets.Label(inRect, "Show filters");
            GUI.color = Color.white;
        }
    }

    private void DrawBottomButtons(Rect inRect)
    {
        if (Listing.Selected != null)
        {
            if (Widgets.ButtonText(new Rect(inRect.xMax - UIUtility.BottomButtonSize.x, inRect.y, UIUtility.BottomButtonSize.x, UIUtility.BottomButtonSize.y), "Add".Translate().CapitalizeFirst()))
            {
                _action(Listing.Selected);
            }

            if (!Widgets.ButtonText(new Rect(inRect.x, inRect.y, UIUtility.BottomButtonSize.x, UIUtility.BottomButtonSize.y), "Close".Translate()))
            {
                return;
            }

            Close();
        }
        else
        {
            if (!Widgets.ButtonText(new Rect((float)((inRect.width - (double)UIUtility.BottomButtonSize.x) / 2.0), inRect.y, UIUtility.BottomButtonSize.x, UIUtility.BottomButtonSize.y), "Close".Translate()))
                return;
            Close();
        }
    }

    private void DrawFilters(Rect inRect)
    {
        var allFilters = Listing.Filters;
        var activeFilters = Listing.ActiveFilters;

        UIUtility.ListSeparator(inRect.TakeTopPart(Text.LineHeightOf(GameFont.Small) + 8f), $"{"PawnEditor.Filters".Translate().CapitalizeFirst()}");
        string label1 = "Add".Translate().CapitalizeFirst() + " " + "PawnEditor.Filter".Translate().ToLower() + "...";
        string label2 = "RemoveOrgan".Translate().CapitalizeFirst() + " " + "All".Translate().ToLower();
        Rect buttonRect = inRect.TakeTopPart(UIUtility.RegularButtonHeight);

        var list = new List<FloatMenuOption>();

        foreach (TFilter<T> filter in allFilters)
        {
            {
                list.Insert(0, new FloatMenuOption(filter.Label, delegate
                {
                    int maxFilterCount = allFilters.Count(f => f.Label == filter.Label);

                    if (activeFilters.Count(f => f.Label == filter.Label) < maxFilterCount)
                    {
                        activeFilters.Add(allFilters.FirstOrDefault(f => f.Label == filter.Label && !activeFilters.Contains(f)));
                    }
                    else
                    {
                        Messages.Message(new Message("Reached limit of this specific filter count", MessageTypeDefOf.RejectInput));
                    }
                }));
            }
        }

        var distinctList = list.GroupBy(l => l.Label).Select(m => m.First()).OrderBy(n => n.Label).ToList();

        if (Widgets.ButtonText(buttonRect.TakeLeftPart(inRect.width * 0.7f), label1))
        {
            Find.WindowStack.Add(new FloatMenu(distinctList));
        }

        buttonRect.xMin += 4f;
        if (Widgets.ButtonText(buttonRect, label2))
        {
            activeFilters.ForEach(f => f.DoInvert = false);
            activeFilters.Clear();
        }

        inRect.yMin += 4f;

        List<TFilter<T>> filtersToRemove = new List<TFilter<T>>();
        foreach (var activeFilter in activeFilters)
        {
            activeFilter.DrawFilter(ref inRect, ref filtersToRemove);
            inRect.yMin += 4f;
        }


        filtersToRemove.ForEach(ftr => activeFilters.Remove(ftr));
    }

    private void DrawNodeCollapse(Rect inRect)
    {
        if (Widgets.ButtonText(inRect.RightHalf(), "OpenFolder".Translate() + " " + "AllDays".Translate()))
        {
            foreach (var treeNodeThingCategory in TreeNodeThingCategory.ChildCategoryNodes)
            {
                treeNodeThingCategory.SetOpen(1, true);
                foreach (var child in treeNodeThingCategory.ChildCategoryNodes)
                {
                    child.SetOpen(1, true);
                }
            }
        }

        if (Widgets.ButtonText(inRect.LeftHalf(), "Close".Translate() + " " + "AllDays".Translate()))
        {
            foreach (var treeNodeThingCategory in TreeNodeThingCategory.ChildCategoryNodes)
            {
                treeNodeThingCategory.SetOpen(1, false);
                foreach (var child in treeNodeThingCategory.ChildCategoryNodes)
                {
                    child.SetOpen(1, false);
                }
            }
        }
    }

    private void UpdateWindowRect()
    {
        if (_showFilters && windowRect.width != WideSize.x)
        {
            windowRect.width = WideSize.x;
            windowRect.height = WideSize.y;
            Log.Message("wide");
        }
        else if (!_showFilters && windowRect.width != InitialSize.x)
        {
            windowRect.width = InitialSize.x;
            windowRect.height = InitialSize.y;
            Log.Message("small");
        }
    }

    private void CloseIfNotSelected()
    {
        if (Find.WindowStack.focusedWindow is Dialog_PawnEditor)
        {
            Find.WindowStack.TryRemove(this);
        }
    }
}