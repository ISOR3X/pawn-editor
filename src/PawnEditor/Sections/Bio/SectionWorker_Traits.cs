using HotSwap;
using PawnEditor.Table;
using PawnEditor.Table.Filters.AbilityDef;
using Taffy;
using RimWorld;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Col = PawnEditor.Table.ColumnWorker<PawnEditor.TraitUtility.TraitRecord>;
using Display = Taffy.Display;
using FlexDirection = Taffy.FlexDirection;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Traits(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        var traits = pawn.story.traits.TraitsSorted;
        var incapableOf = CharacterCardUtility.WorkTagsFrom(pawn.CombinedDisabledWorkTags).ToList();

        builder.Div(
            row =>
            {
                DoTraits(row, traits, pawn);
                DoIncapableOf(row, incapableOf, pawn);
            },
            new StyleOverride
            {
                width = Dimension.Percent(1f),
                flexWrap = FlexWrap.Wrap,
                flexDirection = FlexDirection.Row,
                gap = Void.Taffy.Gap(GenUI.GapTiny),
            }
        );
        builder.Button("Add trait",
            onClick: _ =>
            {
                Find.WindowStack.Add(new Window_Table<TraitUtility.TraitRecord>(GetTraitsTable(pawn),
                    Find.WindowStack.WindowOfType<Window_Editor>(),
                    selectedItemSlot: (b, i) => { b.Text("Selected: " + (i?.Trait.LabelCap ?? "None")); }
                ));
            }
        );
    }

    private static void DoElementRect(TaffyBuilder builder, (Color?, string, string?) metaData,
        Action<Rect>? onClick = null)
    {
        var (textColor, label, tooltip) = metaData;
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
            inner => { inner.Text(label, color: textColor ?? Color.white, anchor: TextAnchor.MiddleCenter); },
            new StyleOverride
            {
                padding = new Rect<LengthPercentage>(5f, 5f, 0f, 0f),
                height = Dimension.AUTO
            });
    }


    private static void DoElementsRect<T>(TaffyBuilder builder, string headerLabel, List<T>? items,
        Func<T, (Color, string, string)> itemMetaGetter, string emptyLabel = "None", Action<T>? onClick = null)
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
                flexBasis = 400f,
                flexShrink = 0f,
            });
    }

    private static void DoTraits(TaffyBuilder builder, List<Trait>? traits, Pawn pawn)
    {
        var emptyLabel = pawn.DevelopmentalStage.Baby()
            ? "TraitsDevelopLaterBaby".Translate()
            : "None".Translate();
        DoElementsRect(builder, "Traits", traits,
            t => (GetTraitTextColor(t), t.LabelCap, t.TipString(pawn)), emptyLabel, t => OnClick(t, pawn));
        return;

        static Color GetTraitTextColor(Trait trait)
        {
            if (trait.Suppressed) return ColoredText.SubtleGrayColor;
            if (trait.sourceGene != null) return ColoredText.GeneColor;
            return Color.white;
        }

        static void OnClick(Trait trait, Pawn pawn)
        {
            if (Event.current.shift)
                Messages.Message("Deletion is not implemented yet for traits", MessageTypeDefOf.RejectInput);
        }
    }

    private static void DoIncapableOf(TaffyBuilder builder, List<WorkTags>? workTags, Pawn pawn)
    {
        DoElementsRect(builder, "Incapable of", workTags,
            t => (CharacterCardUtility.GetDisabledWorkTagLabelColor(pawn, t), t.LabelTranslated().CapitalizeFirst(),
                GetTooltip(t, pawn)));
        return;

        static string GetTooltip(WorkTags t, Pawn pawn) =>
            CharacterCardUtility.GetWorkTypeDisabledCausedBy(pawn, t) + "\n" +
            CharacterCardUtility.GetWorkTypesDisabledByWorkTag(t);
    }

    private static Table<TraitUtility.TraitRecord> GetTraitsTable(Pawn pawn)
    {
        return new Table<TraitUtility.TraitRecord>(
            rows: TraitUtility.AllTraits,
            columns:
            [
                Col.Create(
                    Void.Taffy.Fr(),
                    (grid, record) => grid.Text(record.Degree.LabelCap),
                    "Label"
                ),
                Col.Create(
                    Void.Taffy.Fr(),
                    (grid, record) => grid.Text(record.TraitDef.modContentPack?.Name ?? "",
                        color: ColoredText.SubtleGrayColor),
                    "Source"
                ),
            ],
            onRowHover: (rowRect, rowTrait, ctx) =>
            {
                if (ctx is not PawnContext pawnCtx) return;
                var tip = rowTrait.Trait.TipString(pawnCtx.Value);
                TooltipHandler.TipRegion(rowRect, tip);
            },
            context: new PawnContext(pawn),
            searchProjection: record => record.Degree.LabelCap
        );
    }
}