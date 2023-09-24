using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

// TODO: Listing_TreeThing as child of Listing_Thing.
public class Listing_Thing<T> : Listing_Tree
{
    public List<T> FilteredThings;
    protected readonly QuickSearchFilter SearchFilter;
    protected Rect VisibleRect;

    private readonly Pawn _pawn;
    public T SelectedThing;

    public bool showIcon;

    public Listing_Thing(
        QuickSearchFilter searchFilter,
        List<T> things, bool showIcon = true)
    {
        FilteredThings = things;
        lineHeight = 32f;
        SearchFilter = searchFilter;
        this.showIcon = showIcon;
        nestIndentWidth /= 2;
    }

    public Listing_Thing(
        QuickSearchFilter searchFilter,
        List<T> things, Pawn pawn, bool showIcon = true) : this(searchFilter, things, showIcon)
    {
        _pawn = pawn;
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
        foreach (var thing in FilteredThings.Where(thing => Visible(thing) && !HideThingDueToSearch(thing)))
        {
            DoThing(thing, -3);
        }

        return;

        bool HideThingDueToSearch(T thing) => SearchFilter.Active && !SearchFilter.Matches(GetThingLabel(thing));
    }

    protected void DoThing(T thing, int nestLevel)
    {
        Color? nullable = new Color?();
        if (!SearchFilter.Matches(GetThingLabel(thing)))
            nullable = Listing_TreeThingFilter.NoMatchColor;

        if (showIcon)
        {
            nestLevel += 5;
            DrawThingIcon(thing, nestLevel, nullable);
        }

        if (CurrentRowVisibleOnScreen())
        {
            string tipText = GetThingDesc(thing);

            LabelLeft(GetThingLabel(thing), tipText, nestLevel, XAtIndentLevel(nestLevel), nullable);

            bool checkOn = SelectedThing != null && ReferenceEquals(SelectedThing, thing);

            if (Widgets.ButtonInvisible(new Rect(XAtIndentLevel(nestLevel), curY, ColumnWidth, 32f)) || Widgets.RadioButton(new Vector2(LabelWidth, curY + (32f - Widgets.RadioButtonSize) / 2), checkOn))
            {
                SelectedThing = thing;
            }
        }

        EndLine();
    }

    protected virtual bool Visible(T thing) => FilteredThings.Contains(thing);

    protected bool CurrentRowVisibleOnScreen() => VisibleRect.Overlaps(new Rect(0.0f, curY, ColumnWidth, lineHeight));

    public static string GetThingLabel(T thing)
    {
        if (typeof(Dialog_SelectPawnTrait.TraitInfo).IsAssignableFrom(typeof(T)))
        {
            var t = thing as Dialog_SelectPawnTrait.TraitInfo;
            return t?.TraitDegreeData.LabelCap;
        }

        if (typeof(BackstoryDef).IsAssignableFrom(typeof(T)))
        {
            var t = thing as BackstoryDef;
            return t?.title.CapitalizeFirst();
        }

        if (typeof(Def).IsAssignableFrom(typeof(T)))
        {
            var t = thing as Def;
            return t?.LabelCap;
        }

        return "PLACEHOLDER";
    }

    private string GetThingDesc(T thing)
    {
        if (typeof(Dialog_SelectPawnTrait.TraitInfo).IsAssignableFrom(typeof(T)))
        {
            var t = thing as Dialog_SelectPawnTrait.TraitInfo;
            if (_pawn == null)
            {
                Log.ErrorOnce("No pawn found", 14798);
            }

            return t?.TraitDegreeData.description.Formatted(_pawn.Named("PAWN")).AdjustedFor(_pawn);
        }

        if (typeof(ThingDef).IsAssignableFrom(typeof(T)))
        {
            var t = thing as ThingDef;
            return t?.DescriptionDetailed;
        }
        
        if (typeof(AbilityDef).IsAssignableFrom(typeof(T)))
        {
            var t = thing as AbilityDef;
            return t?.GetTooltip(_pawn);
        }

        if (typeof(BackstoryDef).IsAssignableFrom(typeof(T)))
        {
            var t = thing as BackstoryDef;
            return t?.FullDescriptionFor(_pawn).Resolve();
        }

        if (typeof(Def).IsAssignableFrom(typeof(T)))
        {
            var t = thing as Def;
            return t?.description;
        }

        return "PLACEHOLDER";
    }

    private void DrawThingIcon(T thing, int nestLevel, Color? nullable)
    {
        if (thing is Def def)
        {
            if (thing is AbilityDef abilityDef)
            {
                Widgets.DrawTextureFitted(new Rect(XAtIndentLevel(nestLevel) - 16f, curY, 32f, 32f), abilityDef.uiIcon, .8f);
            }

            Widgets.DefIcon(new Rect(XAtIndentLevel(nestLevel) - 16f, curY, 32f, 32f), def, drawPlaceholder: true, color: nullable, scale: 0.8f);
        }
        else
        {
            Widgets.DrawTextureFitted(new Rect(XAtIndentLevel(nestLevel) - 16f, curY, 32f, 32f), Widgets.PlaceholderIconTex, .8f);
        }
    }

    public static void DrawThingIcon(Rect rect, T thing)
    {
        if (thing is Def def)
        {
            if (thing is AbilityDef abilityDef)
            {
                Widgets.DrawTextureFitted(rect, abilityDef.uiIcon, .8f);
            }

            Widgets.DefIcon(rect, def, drawPlaceholder: true, color: null, scale: 0.8f);
        }
        else
        {
            Widgets.DrawTextureFitted(rect, Widgets.PlaceholderIconTex, .8f);
        }
    }
}