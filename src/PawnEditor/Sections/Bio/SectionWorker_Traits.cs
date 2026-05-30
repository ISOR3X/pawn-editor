using PawnEditor.Table;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;
using Col = PawnEditor.Table.ColumnWorker<PawnEditor.TraitUtility.TraitRecord>;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_Traits(SectionDef def) : SectionWorker(def)
{
    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        var traitsDiv = layout.ComponentById<DivElement>("traits_block");
        var incapableOfDiv = layout.ComponentById<DivElement>("incapableOf_block");

        DoTraits(traitsDiv, pawn);
        DoIncapableOf(incapableOfDiv, pawn);

        layout.ComponentById<ButtonElement>("add").OnClick = _ =>
        {
            Find.WindowStack.Add(new Window_Table<TraitUtility.TraitRecord>(GetTraitsTable(pawn),
                Find.WindowStack.WindowOfType<Window_Editor>(),
                selectedItemSlot: (b, i) => { b.Text("Selected: " + (i?.Trait.LabelCap ?? "None")); }
            ));
        };
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
                padding = new TaffyEdges(Dimension.Px(0f), Dimension.Px(5f), Dimension.Px(0f), Dimension.Px(5f)),
                height = Dimension.Auto()
            });
    }

    private static void DoElementsRect<T>(DivElement div, List<T>? items,
        Func<T, (Color, string, string)> itemMetaGetter, string emptyLabel = "None", Action<T>? onClick = null)
    {
        div.Draw = r => GUI.DrawTexture(r, InspectPaneFiller.HealthTex);
        div.Children =
            traitsBuilder =>
            {
                if (items is { Count: > 0 })
                    foreach (var item in items)
                        DoElementRect(traitsBuilder, itemMetaGetter(item), _ => onClick?.Invoke(item));
                else traitsBuilder.Text(emptyLabel, color: ColoredText.SubtleGrayColor);
            };
    }

    private static void DoTraits(DivElement div, Pawn pawn)
    {
        var traits = pawn.story.traits.TraitsSorted;

        var emptyLabel = pawn.DevelopmentalStage.Baby()
            ? "TraitsDevelopLaterBaby".Translate()
            : "None".Translate();
        DoElementsRect(div, traits, t => (GetTraitTextColor(t), t.LabelCap, t.TipString(pawn)), emptyLabel,
            t => OnClick(t, pawn));
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

    private static void DoIncapableOf(DivElement div, Pawn pawn)
    {
        var workTags = CharacterCardUtility.WorkTagsFrom(pawn.CombinedDisabledWorkTags).ToList();

        DoElementsRect(div, workTags,
            t => (CharacterCardUtility.GetDisabledWorkTagLabelColor(pawn, t), t.LabelTranslated().CapitalizeFirst(),
                GetTooltip(t, pawn)));
        return;

        static string GetTooltip(WorkTags t, Pawn pawn)
        {
            return CharacterCardUtility.GetWorkTypeDisabledCausedBy(pawn, t) + "\n" +
                   CharacterCardUtility.GetWorkTypesDisabledByWorkTag(t);
        }
    }

    private static Table<TraitUtility.TraitRecord> GetTraitsTable(Pawn pawn)
    {
        return new Table<TraitUtility.TraitRecord>(
            TraitUtility.AllTraits,
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
                )
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