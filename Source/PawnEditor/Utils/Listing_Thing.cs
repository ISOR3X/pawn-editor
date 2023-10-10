using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

// If the listing turns out to slow, this is because of the function calls in the lambda expressions. Previously this was mainly the description function.
public class Listing_Thing<T> : Listing_Tree
{
    private readonly List<T> _items;
    protected readonly QuickSearchWidget SearchFilter = new();
    protected Rect VisibleRect;

    public List<TFilter<T>> Filters;
    public readonly List<TFilter<T>> ActiveFilters = new();

    public readonly Func<T, string> LabelGetter;
    private readonly Func<T, string> _descGetter;
    public readonly Action<T, Rect> IconDrawer;

    public T Selected;

    protected Listing_Thing(List<T> items, Func<T, string> labelGetter, Func<T, string> descGetter = null, List<TFilter<T>> filters = null)
    {
        _items = items;
        LabelGetter = labelGetter;
        _descGetter = descGetter;

        if (filters != null)
        {
            Filters = filters;
            ActiveFilters = filters.Where(f => f.EnabledByDefault).ToList();
        }

        lineHeight = 32f;
        nestIndentWidth /= 2;
        verticalSpacing = 0f;
    }

    public Listing_Thing(List<T> items, Func<T, string> labelGetter, Action<T, Rect> iconDrawer, Func<T, string> descGetter = null, List<TFilter<T>> filters = null) :
        this(items, labelGetter, descGetter, filters)
    {
        IconDrawer = iconDrawer;
    }

    public void ListChildren(
        Rect visibleRect)
    {
        VisibleRect = visibleRect;
        ColumnWidth = visibleRect.width - 16f;
        DoCategoryChildren();
    }

    private void DoCategoryChildren()
    {
        int i = 0;
        foreach (var thing in _items.Where(thing => Visible(thing) && !HideThingDueToSearch(thing) && !HideThingDueToFilter(thing)))
        {
            DoThing(thing, -3, i);
            i++;
        }

        return;

        bool HideThingDueToSearch(T thing) => SearchFilter.filter.Active && !SearchFilter.filter.Matches(LabelGetter(thing));

        bool HideThingDueToFilter(T thing) => !ActiveFilters.All(lf => lf.FilterAction(thing));
    }

    protected void DoThing(T thing, int nestLevel, int i)
    {
        Color? nullable = new Color?();
        if (!SearchFilter.filter.Matches(LabelGetter(thing)))
            nullable = Listing_TreeThingFilter.NoMatchColor;

        if (IconDrawer != null)
        {
            nestLevel += 5;
            IconDrawer.Invoke(thing, new Rect(XAtIndentLevel(nestLevel) - 16f, curY, 32f, 32f));
        }

        if (CurrentRowVisibleOnScreen())
        {
            var rect = new Rect(0.0f, curY, ColumnWidth, lineHeight);
            rect.xMin = XAtIndentLevel(nestLevel) + 18f;

            string tipText = string.Empty;
            if (Mouse.IsOver(rect))
            {
                tipText = _descGetter != null ? _descGetter(thing) : string.Empty;
            }

            LabelLeft(LabelGetter(thing), tipText, nestLevel, XAtIndentLevel(nestLevel), nullable);

            bool checkOn = Selected != null && ReferenceEquals(Selected, thing);

            if (Widgets.ButtonInvisible(rect))
            {
                Selected = thing;
            }

            if (checkOn)
            {
                Widgets.DrawHighlightSelected(rect);
            }

            if (i % 2 == 1)
            {
                Widgets.DrawLightHighlight(rect);
            }
        }

        EndLine();
    }

    protected virtual bool Visible(T td)
    {
        bool output = _items.Contains(td);
        if (ActiveFilters.Any())
            output = output && ActiveFilters.All(lf => lf.FilterAction(td));

        return output;
    }

    protected bool CurrentRowVisibleOnScreen() => VisibleRect.Overlaps(new Rect(0.0f, curY, ColumnWidth, lineHeight));

    public void DrawSearchBar(Rect inRect)
    {
        SearchFilter.OnGUI(inRect);
    }
}