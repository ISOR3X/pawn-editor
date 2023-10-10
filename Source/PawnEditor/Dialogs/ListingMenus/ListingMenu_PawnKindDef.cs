using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public class ListingMenu_PawnKindDef : ListingMenu<PawnKindDef>
{
    private static readonly Func<PawnCategory, List<PawnKindDef>> output = GetPawnList;

    private static PawnCategory type;
    private static List<PawnKindDef> animals;
    private static List<PawnKindDef> mechs;
    private static List<PawnKindDef> humans;
    private static List<PawnKindDef> all;

    private static readonly Func<PawnKindDef, string> labelGetter = p => p.LabelCap;
    private static readonly Func<PawnKindDef, string> descGetter = p => p.race.description;
    private static readonly Action<PawnKindDef, Rect> iconDrawer = DrawPawnIcon;

    static ListingMenu_PawnKindDef()
    {
        MakePawnLists();
    }

    public ListingMenu_PawnKindDef(PawnCategory pawnCategory, Action<PawnKindDef> addAction) : base(output.Invoke(pawnCategory), labelGetter, addAction,
        "ChooseStuffForRelic".Translate() + " " + "PawnEditor.PawnKindDef".Translate(), descGetter, iconDrawer)
    {
        type = pawnCategory;
    }

    private static void DrawPawnIcon(PawnKindDef pawnKindDef, Rect rect)
    {
        var texture = Widgets.PlaceholderIconTex;
        Color color = Color.white;
        if (pawnKindDef != null && type != PawnCategory.Humans)
        {
            var bodyGraphicData = pawnKindDef.lifeStages.LastOrDefault()!.bodyGraphicData;
            color = bodyGraphicData.color;
            texture = ContentFinder<Texture2D>.Get(bodyGraphicData.texPath + "_east");
        }

        GUI.color = color;
        Widgets.DrawTextureFitted(rect, texture, .8f);
        GUI.color = Color.white;
    }


    private static List<PawnKindDef> GetPawnList(PawnCategory pawnCategory)
    {
        switch (pawnCategory)
        {
            case PawnCategory.Animals:
            {
                return animals;
            }
            case PawnCategory.Mechs:
            {
                return mechs;
            }
            default:
            {
                return humans;
            }
        }
    }

    private static void MakePawnLists()
    {
        all = DefDatabase<PawnKindDef>.AllDefs.Distinct().ToList();
        animals = all.Where(pkd => pkd.race.race.Animal && !pkd.race.race.Dryad)
            .ToList();
        mechs = all.Where(pkd => // Right now mechanoids are found based on their maskPath but this seems a bit weird.
                pkd.race.race.IsMechanoid &&
                pkd.lifeStages.LastOrDefault()!.bodyGraphicData.maskPath != null)
            .ToList();
        humans = all.Where(pk => pk.RaceProps.Humanlike)
            .ToList();
    }
}