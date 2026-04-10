using HotSwap;
using PawnEditor.Table;
using PawnEditor.Table.Filters.AbilityDef;
using Taffy;
using RimWorld;
using UnityEngine;
using Verse;
using Col = PawnEditor.Table.ColumnWorker<RimWorld.AbilityDef>;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Abilities(SectionDef def) : SectionWorker(def)
{
    private const float AbilitiesHeight = 36f;
    private float? _abilitiesHeight;

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        builder.Text("Abilities", color: ColoredText.TipSectionTitleColor);

        builder.Item(
            r =>
            {
                DoAbilitiesRect(r, pawn);
                _abilitiesHeight = GetAbilitiesHeight(pawn, r.width);
            }, new StyleOverride
            {
                width = Dimension.Percent(1f),
                height = _abilitiesHeight ?? AbilitiesHeight,
                flexGrow = 1f
            });

        builder.Button("Add ability",
            onClick: _ =>
                Find.WindowStack.Add(new Window_Table<AbilityDef>(GetTraitsTable(pawn),
                    Find.WindowStack.WindowOfType<Window_Editor>(),
                    selectedItemSlot: (b, i) => { b.Text("Selected: " + (i?.LabelCap ?? "None")); }))
        );
    }

    private static Table<AbilityDef> GetTraitsTable(Pawn pawn)
    {
        return new Table<AbilityDef>(
            rows: DefDatabase<AbilityDef>.AllDefsListForReading,
            columns:
            [
                Col.Create(
                    Taffy.Px(20f),
                    (grid, def) => grid.Icon(def.uiIcon)
                ),
                Col.CreateText(
                    Taffy.Fr(), def => def.LabelCap, "Label"
                ),
                Col.CreateText(
                    Taffy.Fr(),
                    def => def.modContentPack.Name,
                    "Source",
                    color: ColoredText.SubtleGrayColor
                ),
            ],
            onRowHover: (rowRect, abilityDef, ctx) =>
            {
                if (ctx is not PawnContext pawnContext) return;
                var tip = abilityDef.GetTooltip(pawnContext.Value);
                TooltipHandler.TipRegion(rowRect, tip);
            },
            context: new PawnContext(pawn),
            filters: [new RowFilter_DefContentSource<AbilityDef>(), new RowFilter_Level()],
            searchProjection: def => def.LabelCap
        );
    }

    private static float GetAbilitiesHeight(Pawn pawn, float width)
    {
        var abilities = GetAbilities(pawn);
        return UIUtility.DrawElementStackSectionHeight(abilities, _ => AbilitiesHeight, width, AbilitiesHeight);
    }

    private static void DoAbilitiesRect(Rect inRect, Pawn pawn)
    {
        UIUtility.DrawElementStackSection(inRect, GetAbilities(pawn),
            (r, ability) =>
            {
                GUI.DrawTexture(r, BaseContent.ClearTex);
                if (Mouse.IsOver(r)) Verse.Widgets.DrawHighlight(r);
                if (Verse.Widgets.ButtonImage(r, ability.def.uiIcon, false))
                {
                    if (Event.current.shift) TryDeleteAbility(ability.def, pawn);
                    else Find.WindowStack.Add(new Dialog_InfoCard(ability.def));
                }

                if (Mouse.IsOver(r))
                    TooltipHandler.TipRegion(r, new TipSignal(() =>
                            ability.Tooltip + "\n\n" +
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