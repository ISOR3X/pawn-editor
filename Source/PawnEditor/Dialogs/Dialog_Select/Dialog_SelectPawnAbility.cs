using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public class Dialog_SelectPawnAbility : Dialog_SelectThing<AbilityDef>
{
    public Dialog_SelectPawnAbility(Pawn pawn) : base(DefDatabase<AbilityDef>.AllDefs.ToList(), pawn)
    {
        Listing = new(_quickSearchWidget.filter, ThingList, CurPawn);

        OnSelected = abilityDef =>
        {
            if (abilityDef.IsPsycast && !CurPawn.HasPsylink)
            {
                var addPsylink = () => { CurPawn.health.AddHediff(HediffDefOf.PsychicAmplifier, CurPawn.health.hediffSet.GetBrain()); };
                Find.WindowStack.Add(new Dialog_MessageBox("PawnEditor.AddPsylink".Translate(abilityDef.LabelCap), "Yes".Translate(), addPsylink,
                    "No".Translate(), acceptAction: addPsylink));
            }

            CurPawn.abilities.GainAbility(abilityDef);
        };
    }

    protected override string PageTitle => "ChooseStuffForRelic".Translate() + " " + "PawnEditor.Ability".Translate().ToLower();

    protected override List<TFilter<AbilityDef>> Filters()
    {
        var filters = base.Filters();

        var abilityDefLevels = DefDatabase<AbilityDef>.AllDefs.Select(ad => ad.level).Distinct().ToList();

        filters.Add(new("PawnEditor.MinAbilityLevel".Translate(), false, item => item.level, abilityDefLevels.Min(), abilityDefLevels.Max(),
            "PawnEditor.MinAbilityLevelDesc".Translate()));

        return filters;
    }

    protected override void DrawInfoCard(ref Rect inRect)
    {
        base.DrawInfoCard(ref inRect);
        var outRect = GenUI.DrawElementStack(inRect, 32f, CurPawn.abilities.abilities, delegate(Rect r, Ability ability)
        {
            // GUI.DrawTexture(r, Command.BGTexShrunk);
            if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);

            if (Widgets.ButtonImage(r, ability.def.uiIcon, false)) Find.WindowStack.Add(new Dialog_InfoCard(ability.def));

            if (Mouse.IsOver(r))
            {
                var abilCapture = ability;
                TooltipHandler.TipRegion(r, () => abilCapture.Tooltip + "\n\n" + "ClickToLearnMore".Translate().Colorize(ColoredText.SubtleGrayColor),
                    r.GetHashCode());
            }
        }, _ => 32f);
        inRect.yMin += outRect.height + 16f; // To update inRect
    }
}
