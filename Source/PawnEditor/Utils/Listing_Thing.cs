using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

// If the listing turns out too slow, this is because of the function calls in the lambda expressions. Previously this was mainly the description function.
public class Listing_Thing<T> : Listing_Tree
{
    public List<Filter<T>> ActiveFilters = new();
    public readonly Action<T, Rect> IconDrawer;

    public readonly Func<T, string> LabelGetter;
    protected readonly QuickSearchWidget SearchFilter = new();
    private readonly bool _allowMultiSelect;
    private readonly HashSet<T> _auxHighlight;
    private readonly Func<T, string> _descGetter;
    private readonly List<T> _items;
    private readonly int _maxCount;

    public Action<Rect, T, bool> DoThingExtras;

    public List<Filter<T>> Filters;
    public List<T> MultiSelected;

    public T Selected;
    protected Rect VisibleRect;

    public Listing_Thing(List<T> items, Func<T, string> labelGetter, Func<T, string> descGetter = null,
        List<Filter<T>> filters = null, IEnumerable<T> auxHighlight = null) : this(items, labelGetter, null, descGetter, filters, auxHighlight)
    {
    }

    public Listing_Thing(List<T> items, int maxCount, Func<T, string> labelGetter, Action<T, Rect> iconDrawer = null, Func<T, string> descGetter = null,
        List<Filter<T>> filters = null, IEnumerable<T> auxHighlight = null) :
        this(items, labelGetter, iconDrawer, descGetter, filters, auxHighlight)
    {
        _allowMultiSelect = true;
        _maxCount = maxCount;
        MultiSelected = new();
    }

    public Listing_Thing(List<T> items, Func<T, string> labelGetter, Action<T, Rect> iconDrawer = null, Func<T, string> descGetter = null,
        List<Filter<T>> filters = null, IEnumerable<T> auxHighlight = null)
    {
        _items = items;
        LabelGetter = labelGetter;
        _descGetter = descGetter;
        IconDrawer = iconDrawer;

        if (filters != null)
        {
            Filters = filters;
            if (ActiveFilters.Count == 0) ActiveFilters = filters.Where(f => f.EnabledByDefault).ToList();
        }

        lineHeight = 32f;
        nestIndentWidth /= 2;
        verticalSpacing = 0f;

        _auxHighlight = auxHighlight?.ToHashSet() ?? new HashSet<T>();
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
        var i = 0;
        foreach (var thing in _items.Where(thing => Visible(thing) && !HideThingDueToSearch(thing) && !HideThingDueToFilter(thing)))
        {
            DoThing(thing, -3, i);
            i++;
        }

        return;

        bool HideThingDueToSearch(T thing) => SearchFilter.filter.Active && !SearchFilter.filter.Matches(LabelGetter(thing));

        bool HideThingDueToFilter(T thing) => !ActiveFilters.All(lf => lf.Matches(thing));
    }

    protected void DoThing(T thing, int nestLevel, int i)
    {
        var nullable = new Color?();
        var label = LabelGetter(thing);
        if (!SearchFilter.filter.Matches(label))
            nullable = Listing_TreeThingFilter.NoMatchColor;

        if (IconDrawer != null)
        {
            nestLevel += 5;
            IconDrawer(thing, new(XAtIndentLevel(nestLevel) - 16f, curY, 32f, 32f));
        }

        if (CurrentRowVisibleOnScreen())
        {
            var rect = new Rect(0.0f, curY, ColumnWidth, lineHeight);
            rect.xMin = XAtIndentLevel(nestLevel) + 18f;

            var tipText = string.Empty;
            if (Mouse.IsOver(rect)) tipText = _descGetter != null ? _descGetter(thing) : string.Empty;

            LabelLeft(label, tipText, nestLevel, XAtIndentLevel(nestLevel), nullable);

            var selected = _allowMultiSelect ? MultiSelected.Contains(thing) : Selected != null && ReferenceEquals(Selected, thing);

            if (Widgets.ButtonInvisible(rect))
            {
                if (_allowMultiSelect)
                {
                    if (selected)
                        MultiSelected.Remove(thing);
                    else if (MultiSelected.Count < _maxCount)
                        MultiSelected.Add(thing);
                }
                else Selected = thing;
            }


            if (selected) Widgets.DrawHighlightSelected(rect);

            if (_auxHighlight.Contains(thing)) Widgets.DrawHighlight(rect);

            if (i % 2 == 1) Widgets.DrawLightHighlight(rect);

            rect.xMin += XAtIndentLevel(nestLevel);
            rect.xMin += LabelWidth;

            DoThingExtras?.Invoke(rect, thing, selected);
        }

        EndLine();
    }

    protected virtual bool Visible(T td)
    {
        var output = _items.Contains(td);
        if (ActiveFilters.Any())
            output = output && ActiveFilters.All(lf => lf.Matches(td));

        return output;
    }

    protected bool CurrentRowVisibleOnScreen() => VisibleRect.Overlaps(new(0.0f, curY, ColumnWidth, lineHeight));

    public void DrawSearchBar(Rect inRect)
    {
        SearchFilter.OnGUI(inRect);
    }
}