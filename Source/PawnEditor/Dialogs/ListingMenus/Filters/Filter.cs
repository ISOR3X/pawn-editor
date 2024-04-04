using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public abstract class Filter<T>
{
    public readonly string Description;
    public readonly bool EnabledByDefault;
    public readonly string Label;
    public bool Inverted;

    protected Filter(string label, bool enabledByDefault = false, string description = null)
    {
        Label = label;
        EnabledByDefault = enabledByDefault;
        Description = description;
    }

    protected virtual float Height => UIUtility.RegularButtonHeight * 2 + 8;

    public bool DrawFilter(ref Rect inRect)
    {
        var rowHeight = Height;
        //if (_filterType == TFilter<>.FilterType.Toggle) { rowHeight -= (UIUtility.RegularButtonHeight + 4); }

        var filterRect = inRect.TakeTopPart(rowHeight);

        // Grey background
        GUI.color = CharacterCardUtility.StackElementBackground;
        GUI.DrawTexture(filterRect, BaseContent.WhiteTex);
        GUI.color = Color.white;
        filterRect = filterRect.ContractedBy(6f);

        // Filter widget
        DrawWidget(filterRect.TakeBottomPart(UIUtility.RegularButtonHeight));
        filterRect.yMax -= 4f;

        // Filter info
        var topRowRect = filterRect.TakeTopPart(Text.LineHeightOf(GameFont.Small));
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            var buttonRect = topRowRect.TakeRightPart(topRowRect.height);
            if (Widgets.ButtonImage(buttonRect, TexButton.Delete))
            {
                Inverted = false;
                return true;
            }

            buttonRect = topRowRect.TakeRightPart(topRowRect.height).ExpandedBy(4f);
            buttonRect.x -= 4f;

            TooltipHandler.TipRegion(buttonRect, "PawnEditor.InvertFilter".Translate());

            var filter = Inverted ? TexPawnEditor.InvertFilterActive : TexPawnEditor.InvertFilter;
            if (Widgets.ButtonImage(buttonRect, filter)) Inverted = !Inverted;

            Widgets.Label(topRowRect, Label);
            if (Mouse.IsOver(topRowRect) && Description != "")
                TooltipHandler.TipRegion(topRowRect, $"{Label.Colorize(ColoredText.TipSectionTitleColor)}\n\n{Description}");
        }

        return false;
    }

    protected abstract void DrawWidget(Rect rect);

    public bool Matches(T item) => Inverted ? !MatchesInt(item) : MatchesInt(item);

    protected abstract bool MatchesInt(T item);
}
