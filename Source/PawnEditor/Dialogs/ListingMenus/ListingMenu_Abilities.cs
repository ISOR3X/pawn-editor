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
    private static readonly List<Filter<AbilityDef>> filters;

    static ListingMenu_Abilities()
    {
        items = DefDatabase<AbilityDef>.AllDefsListForReading;
        filters = GetFilters();
    }

    public ListingMenu_Abilities(Pawn pawn) : base(items, labelGetter, b => TryAdd(b, pawn),
        "PawnEditor.Choose".Translate() + " " + "PawnEditor.Ability".Translate().ToLower(),
        b => descGetter(b, pawn), DrawIcon, filters, pawn)
    {
    }

    private static void DrawIcon(AbilityDef abilityDef, Rect rect)
    {
        var texture = Widgets.PlaceholderIconTex;
        if (abilityDef != null) texture = abilityDef.uiIcon;

        Widgets.DrawTextureFitted(rect, texture, .8f);
    }

    private static AddResult TryAdd(AbilityDef abilityDef, Pawn pawn)
    {
        if (abilityDef.IsPsycast && !pawn.HasPsylink)
        {
            var addPsylink = () =>
            {
                pawn.health.AddHediff(HediffDefOf.PsychicAmplifier, pawn.health.hediffSet.GetBrain());
                TabWorker_Table<Pawn>.ClearCacheFor<TabWorker_Health>();
            };
            Find.WindowStack.Add(new Dialog_MessageBox("PawnEditor.AddPsylink".Translate(abilityDef.LabelCap), "Yes".Translate(), addPsylink,
                "No".Translate(), acceptAction: addPsylink));
        }

        pawn.abilities.GainAbility(abilityDef);
        return true;
    }

    private static List<Filter<AbilityDef>> GetFilters()
    {
        var list = new List<Filter<AbilityDef>>();

        var abilityDefLevels = DefDatabase<AbilityDef>.AllDefs.Select(ad => ad.level).Distinct().ToList();
        if (abilityDefLevels.Any())
            list.Add(new Filter_IntRange<AbilityDef>("PawnEditor.MinAbilityLevel".Translate(), new(abilityDefLevels.Min(), abilityDefLevels.Max()),
                item => item.level, false, "PawnEditor.MinAbilityLevelDesc".Translate()));
        list.Add(new Filter_ModSource<AbilityDef>());

        return list;
    }
}