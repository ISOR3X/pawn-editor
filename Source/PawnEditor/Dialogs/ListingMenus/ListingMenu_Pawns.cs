using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public class ListingMenu_Pawns : ListingMenu<Pawn>
{
    public ListingMenu_Pawns(List<Pawn> items, Pawn pawn, string nextLabel, Func<Pawn, AddResult> nextAction, string closeLabel = null,
        Action closeAction = null,
        bool highlightGender = false) :
        base(items, p => p.Name?.ToStringShort ?? p.LabelShort, nextAction, "ChooseStuffForRelic".Translate() + " " + "PawnEditor.Pawn".Translate(),
            p => p.DescriptionDetailed,
            DrawPawnIcon, GetFilters(), pawn, nextLabel, closeLabel, closeAction)
    {
        if (highlightGender) Listing.DoThingExtras = DoThingExtras;
    }

    public ListingMenu_Pawns(List<Pawn> items, Pawn pawn, string nextLabel, Func<List<Pawn>, AddResult> nextAction, int count, string closeLabel = null,
        Action closeAction = null, bool highlightGender = false) :
        base(items, p => p.Name?.ToStringShort ?? p.LabelShort, nextAction, "ChooseStuffForRelic".Translate() + " " + Find.ActiveLanguageWorker.Pluralize(
                "PawnEditor.Pawn".Translate(), count), new(count, count),
            p => p.DescriptionDetailed, DrawPawnIcon, GetFilters(), pawn, nextLabel, closeLabel, closeAction)
    {
        if (highlightGender) Listing.DoThingExtras = DoThingExtras;
    }

    public override void PostOpen()
    {
        base.PostOpen();
        TabWorker_Table<Pawn>.ClearCacheFor<TabWorker_Social>();
    }

    private static void DoThingExtras(Rect inRect, Pawn pawn, bool selected)
    {
        if (selected) GUI.DrawTexture(inRect.RightPartPixels(inRect.height).ContractedBy(3), pawn.gender.GetIcon());
    }

    private static void DrawPawnIcon(Pawn pawn, Rect rect)
    {
        Texture texture = Widgets.PlaceholderIconTex;
        if (pawn != null)
            texture = PawnEditor.GetPawnTex(pawn, new(25, 25), Rot4.South, cameraZoom: 2f);
        Widgets.DrawTextureFitted(rect, texture, .8f);
    }

    private static List<Filter<Pawn>> GetFilters()
    {
        var list = new List<Filter<Pawn>>();

        list.Add(new Filter_Toggle<Pawn>("PawnEditor.IsColonist".Translate(), p => p.IsColonist));
        list.Add(new Filter_Toggle<Pawn>("PawnEditor.IsHuman".Translate(), p => p.RaceProps.Humanlike));

        return list;
    }
}
