using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Abilities(SectionDef def) : SectionWorker(def)
{
    public const float AbilitiesHeight = 36f;
    private float _abilitiesHeight;

    protected override void DoSectionContents(TaffyBuilder col, Pawn pawn)
    {
        col.Item(height: Text.LineHeight, draw: r => r.LabelH2("Abilities"));

        col.Item(height: _abilitiesHeight > 0f ? _abilitiesHeight : AbilitiesHeight, draw: r =>
        {
            DoAbilitiesRect(r, pawn);
            _abilitiesHeight = GetAbilitiesHeight(pawn, r.width);
        });

        col.Item(height: 2f);

        col.Item(height: UIUtility.ButtonHeight, draw: r =>
        {
            var w = Text.CalcSize("Add ability").x + 32f;
            Verse.Widgets.ButtonText(r.TakeLeftPart(w), "Add ability");
        });
    }

    private static float GetAbilitiesHeight(Pawn pawn, float width)
    {
        var abilities = GetAbilities(pawn);
        return UIUtility.DrawElementStackSectionHeight(abilities, _ => AbilitiesHeight, width, AbilitiesHeight);
    }

    private static void DoAbilitiesRect(Rect inRect, Pawn pawn)
    {
        UIUtility.DrawElementStackSection(inRect, GetAbilities(pawn),
            (r, abil) =>
            {
                GUI.DrawTexture(r, BaseContent.ClearTex);
                if (Mouse.IsOver(r)) Verse.Widgets.DrawHighlight(r);
                if (Verse.Widgets.ButtonImage(r, abil.def.uiIcon, false))
                {
                    if (Event.current.shift) TryDeleteAbility(abil.def, pawn);
                    else Find.WindowStack.Add(new Dialog_InfoCard(abil.def));
                }

                if (Mouse.IsOver(r))
                    TooltipHandler.TipRegion(r, new TipSignal(() =>
                            abil.Tooltip + "\n\n" +
                            "ClickToLearnMore".Translate().Colorize(ColoredText.SubtleGrayColor) +
                            "\n" + "Shift + left click to delete.".Colorize(ColoredText.SubtleGrayColor),
                        (int)r.y * 37));
            },
            _ => AbilitiesHeight,
            AbilitiesHeight);
    }

    private static List<Ability> GetAbilities(Pawn pawn)
    {
        return pawn.abilities.AllAbilitiesForReading
            .Where(a => a.def.showOnCharacterCard)
            .OrderBy(a => a.def.level)
            .ThenBy(a => a.def.EntropyGain)
            .ToList();
    }

    private static void TryDeleteAbility(AbilityDef abilityDef, Pawn pawn)
    {
        var ability = pawn.abilities.abilities.FirstOrDefault(x => x.def == abilityDef);
        if (ability == null)
        {
            Messages.Message($"Failed to delete ability {abilityDef.defName} (it was not related to a def)",
                MessageTypeDefOf.RejectInput);
            return;
        }

        pawn.abilities.RemoveAbility(abilityDef);
    }
}
