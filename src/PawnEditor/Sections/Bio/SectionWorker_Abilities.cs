using HotSwap;
using PawnEditor.Table;
using PawnEditor.Table.Filters.AbilityDef;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;
using Col = PawnEditor.Table.ColumnWorker<RimWorld.AbilityDef>;
using Layout = Void.Layout;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Abilities(SectionDef def) : SectionWorker(def)
{
    private static List<Ability> GetAbilitiesForPawn(Pawn pawn)
    {
        return pawn.abilities.AllAbilitiesForReading
            .Where(a => a.def.showOnCharacterCard)
            .OrderBy(a => a.def.level)
            .ThenBy(a => a.def.EntropyGain)
            .ToList();
    }

    public override void OnLayout(Layout layout, Pawn pawn)
    {
        layout.ComponentById<DivElement>("abilityIcons").Children = inner =>
            DoAbilities(inner, GetAbilitiesForPawn(pawn), pawn);

        layout.ComponentById<ButtonElement>("addAbility").OnClick = _ =>
            Find.WindowStack.Add(new Window_Table<AbilityDef>(GetTraitsTable(pawn),
                Find.WindowStack.WindowOfType<Window_Editor>(),
                selectedItemSlot: (b, i) => { b.Text("Selected: " + (i?.LabelCap ?? "None")); }));
    }

    private static void DoElementRect(TaffyBuilder builder, (Texture2D, string) metaData, Action<Rect>? onClick = null)
    {
        var (texture, tooltip) = metaData;
        builder.Div(r =>
            {
                Verse.Widgets.DrawRectFast(r, CharacterCardUtility.StackElementBackground);
                if (Mouse.IsOver(r))
                {
                    Verse.Widgets.DrawHighlight(r);
                    if (tooltip != null) TooltipHandler.TipRegion(r, tooltip);
                }

                if (Verse.Widgets.ButtonInvisible(r) && onClick != null) onClick(r);
            },
            inner => inner.Icon(texture, size: UIUtility.ComponentSize.Large),
            new StyleOverride
            {
                padding = Void.Taffy.Padding(5f),
                height = Dimension.AUTO
            });
    }


    private static void DoElementsRect(TaffyBuilder builder, string headerLabel, List<Ability>? items,
        Func<Ability, (Texture2D, string)> itemMetaGetter, string emptyLabel = "None", Action<Ability>? onClick = null)
    {
        builder.Div(
            left =>
            {
                left.Text(headerLabel, color: ColoredText.TipSectionTitleColor);
                left.Div(r => { GUI.DrawTexture(r, InspectPaneFiller.HealthTex); }, traitsBuilder =>
                    {
                        if (items is { Count: > 0 })
                            foreach (var item in items)
                                DoElementRect(traitsBuilder, itemMetaGetter(item), _ => onClick?.Invoke(item));
                        else traitsBuilder.Text(emptyLabel, color: ColoredText.SubtleGrayColor);
                    },
                    new StyleOverride
                    {
                        flexWrap = FlexWrap.Wrap,
                        gap = Void.Taffy.Gap(GenUI.GapTiny),
                        flexGrow = 1f,
                        alignContent = AlignContent.FlexStart,
                        padding = Void.Taffy.Padding(GenUI.GapTiny),
                        width = Dimension.Percent(1f)
                    });
            },
            new StyleOverride
            {
                flexDirection = FlexDirection.Column,
                flexGrow = 1f,
                flexBasis = 200f,
                flexShrink = 0f
            });
    }

    private static void DoAbilities(TaffyBuilder builder, List<Ability>? abilities, Pawn pawn)
    {
        DoElementsRect(builder, "Abilities", abilities, a => (a.def.uiIcon, TooltipGetter(a)),
            onClick: a => OnClick(a, pawn));
        return;

        static string TooltipGetter(Ability a)
        {
            return a.Tooltip + "\n\n" + "ClickToLearnMore".Translate()
                       .Colorize(ColoredText.SubtleGrayColor) + "\n" +
                   "Shift + left click to delete.".Colorize(ColoredText.SubtleGrayColor);
        }

        static void OnClick(Ability ability, Pawn pawn)
        {
            if (Event.current.shift) TryDeleteAbility(ability.def, pawn);
            else Find.WindowStack.Add(new Dialog_InfoCard(ability.def));
        }
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

    private static Table<AbilityDef> GetTraitsTable(Pawn pawn)
    {
        List<IRowFilter<AbilityDef>> filters = [new RowFilter_DefContentSource<AbilityDef>()];
        if (RowFilter_Level.MinMaxRange.max - RowFilter_Level.MinMaxRange.min != 0) filters.Add(new RowFilter_Level());

        return new Table<AbilityDef>(
            DefDatabase<AbilityDef>.AllDefsListForReading,
            [
                Col.Create(
                    Void.Taffy.Px(20f),
                    (grid, def) => grid.Icon(def.uiIcon)
                ),
                Col.CreateText(
                    Void.Taffy.Fr(), def => def.LabelCap, "Label"
                ),
                Col.CreateText(
                    Void.Taffy.Fr(),
                    def => def.modContentPack.Name,
                    "Source",
                    ColoredText.SubtleGrayColor
                )
            ],
            onRowHover: (rowRect, abilityDef, ctx) =>
            {
                if (ctx is not PawnContext pawnContext) return;
                var tip = abilityDef.GetTooltip(pawnContext.Value);
                TooltipHandler.TipRegion(rowRect, tip);
            },
            context: new PawnContext(pawn),
            filters: filters,
            searchProjection: def => def.LabelCap
        );
    }
}