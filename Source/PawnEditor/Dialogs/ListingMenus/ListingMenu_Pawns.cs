using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public class ListingMenu_Pawns : ListingMenu<Pawn>
{
    public ListingMenu_Pawns(List<Pawn> items, Pawn pawn, List<TFilter<Pawn>> filters = null) :
        base(items, p => p.Name.ToStringShort, p => Find.WindowStack.Add(new ListingMenu_Relations(pawn, p, null)),
            "ChooseStuffForRelic".Translate() + " " + "PawnEditor.Pawn".Translate(), p => p.DescriptionDetailed,
            DrawPawnIcon, GetFilters(), pawn)
    {
    }

    private static void DrawPawnIcon(Pawn pawn, Rect rect)
    {
        Texture texture = Widgets.PlaceholderIconTex;
        if (pawn != null)
            texture = PawnEditor.GetPawnTex(pawn, new(25, 25), Rot4.South, cameraZoom: 2f);
        Widgets.DrawTextureFitted(rect, texture, .8f);
    }
    
    private static List<TFilter<Pawn>> GetFilters()
    {
        var list = new List<TFilter<Pawn>>();
        
        list.Add(new("PawnEditor.IsColonist".Translate(), false, p => p.IsColonist));
        list.Add(new("PawnEditor.IsHuman".Translate(), false, p => !p.NonHumanlikeOrWildMan()));

        return list;
    }
}