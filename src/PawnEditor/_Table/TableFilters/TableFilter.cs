using System;
using HotSwap;
using PawnEditor.Extensions;
using UnityEngine;
using Verse;

namespace PawnEditor.Table;

[HotSwappable]
public abstract class TableFilter
{
    private bool _inverted;

    protected abstract string Label { get; }

    public virtual void DrawFilter(Listing_Standard listing, Action? onChanged = null, Action? onRemove = null)
    {
        var rect = listing.GetRect(Text.LineHeight);

        if (onRemove != null)
        {
            if (Verse.Widgets.ButtonText(rect.TakeRightPart(Text.LineHeight), "×"))
                onRemove();
        }

        using (new GUIColor(_inverted ? new Color(1f, 0.6f, 0.2f) : Color.white))
            if (Verse.Widgets.ButtonText(rect.TakeRightPart(Text.LineHeight), "!"))
            {
                _inverted = !_inverted;
                onChanged?.Invoke();
            }

        using (new TextBlock(TextAnchor.MiddleLeft))
            Verse.Widgets.Label(rect, Label.CapitalizeFirst());

        DrawFilterWidget(listing, onChanged);
    }

    protected abstract void DrawFilterWidget(Listing_Standard listing, Action? onChanged = null);

    public bool Matches(Def thing) => _inverted ? !MatchesCore(thing) : MatchesCore(thing);

    protected abstract bool MatchesCore(Def thing);
}