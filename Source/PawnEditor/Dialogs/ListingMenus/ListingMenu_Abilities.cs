using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
public class ListingMenu_Abilities : ListingMenu<AbilityDef>
{
    private static readonly List<AbilityDef> items;
    private static readonly Func<AbilityDef, string> labelGetter = d => d.LabelCap;
    private static readonly Func<AbilityDef, Pawn, string> descGetter = (d, p) => d.GetTooltip(p);
    private static readonly Action<AbilityDef, Rect> iconDrawer = DrawIcon;
    private static readonly Action<AbilityDef, Pawn> action = TryAdd;
    private static readonly List<TFilter<AbilityDef>> filters;

    static ListingMenu_Abilities()
    {
        items = DefDatabase<AbilityDef>.AllDefsListForReading;
        filters = GetFilters();
    }

    public ListingMenu_Abilities(Pawn pawn) : base(items, labelGetter, b => action(b, pawn),
        "ChooseStuffForRelic".Translate() + " " + "PawnEditor.Ability".Translate().ToLower(),
        b => descGetter(b, pawn), iconDrawer, filters, pawn)
    {
    }

    private static void DrawIcon(AbilityDef abilityDef, Rect rect)
    {
        var texture = Widgets.PlaceholderIconTex;
        if (abilityDef != null)
        {
            texture = abilityDef.uiIcon;
        }

        Widgets.DrawTextureFitted(rect, texture, .8f);
    }

    private static void TryAdd(AbilityDef abilityDef, Pawn pawn)
    {
        if (abilityDef.IsPsycast && !pawn.HasPsylink)
        {
            var addPsylink = () => { pawn.health.AddHediff(HediffDefOf.PsychicAmplifier, pawn.health.hediffSet.GetBrain()); };
            Find.WindowStack.Add(new Dialog_MessageBox("PawnEditor.AddPsylink".Translate(abilityDef.LabelCap), "Yes".Translate(), addPsylink,
                "No".Translate(), acceptAction: addPsylink));
        }

        pawn.abilities.GainAbility(abilityDef);
    }

    private static List<TFilter<AbilityDef>> GetFilters()
    {
        var list = new List<TFilter<AbilityDef>>();

        var abilityDefLevels = DefDatabase<AbilityDef>.AllDefs.Select(ad => ad.level).Distinct().ToList();

        list.Add(new("PawnEditor.MinAbilityLevel".Translate(), false, item => item.level, abilityDefLevels.Min(), abilityDefLevels.Max(),
            "PawnEditor.MinAbilityLevelDesc".Translate()));

        return list;
    }
}