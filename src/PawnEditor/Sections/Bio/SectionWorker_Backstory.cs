using HotSwap;
using PawnEditor.Table;
using PawnEditor.TaffySharp;
using RimWorld;
using UnityEngine;
using Verse;
using Col = PawnEditor.Table.ColumnWorker<RimWorld.BackstoryDef>;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Backstory(SectionDef def) : SectionWorker(def)
{
    private const float MaxButtonWidth = 160f;

    protected override void DoSectionContents(TaffyBuilder builder, Pawn pawn)
    {
        string childhoodLabel = "Childhood".Translate();
        string adulthoodLabel = "Adulthood".Translate();

        builder.Text("Backstory", color: ColoredText.TipSectionTitleColor);
        builder.Div(new Style { gap = Taffy.Gap(GenUI.GapSmall, GenUI.GapTiny), flexWrap = FlexWrap.Wrap }, row =>
        {
            DoBackstoryItem(row, pawn, BackstorySlot.Childhood, childhoodLabel);
            DoBackstoryItem(row, pawn, BackstorySlot.Adulthood, adulthoodLabel);
        });
    }

    private static void DoBackstoryItem(TaffyBuilder row, Pawn pawn, BackstorySlot slot, string label)
    {
        var backstory = pawn.story.GetBackstory(slot);
        var buttonLabel = backstory != null ? backstory.TitleCapFor(pawn.gender) : "None".Translate().ToString();

        Action<Rect>? onHover = null;
        if (backstory != null)
        {
            onHover = r =>
            {
                var tip = buttonLabel.Colorize(ColoredText.TipSectionTitleColor) + "\n\n";
                var desc = backstory.FullDescriptionFor(pawn).Resolve();
                TooltipHandler.TipRegion(r, tip + desc);
            };
        }

        row.Div(row2 =>
        {
            row2.Text(label, style: new Style { margin = new Rect<LengthPercentageAuto>(0f, GenUI.GapLabel, 0f, 0f) });
            row2.Button(buttonLabel, onClick: _ =>
                {
                    var sourceFilter = new ContentSourceFilter<BackstoryDef>();
                    var compatFilter = new PawnCompatibleFilter();
                    var t = new Table<BackstoryDef>(
                        rows: DefDatabase<BackstoryDef>.AllDefs.Where(td => td.slot == slot),
                        columns:
                        [
                            Col.Create(
                                Taffy.Px(150f),
                                (grid, def) => grid.Text(def.defName),
                                "Def Name"
                            ),
                            Col.Create<PawnContext>(
                                Taffy.Fr(),
                                (grid, def, ctx) => grid.Text(def.TitleCapFor(ctx.Value.gender)),
                                "Title"
                            ),
                            Col.Create(
                                Taffy.Px(150f),
                                (grid, def) => grid.Text(def.modContentPack?.Name ?? "",
                                    color: ColoredText.SubtleGrayColor),
                                "Mod"
                            ),
                        ],
                        onRowHover: (rowRect, rowBackstory, ctx) =>
                        {
                            if (ctx is not ITableContext<Pawn> pawnCtx) return;
                            var tip = rowBackstory.TitleCapFor(pawnCtx.Value.gender).Colorize(ColoredText.TipSectionTitleColor) + "\n\n";
                            var desc = rowBackstory.FullDescriptionFor(pawn).Resolve();
                            TooltipHandler.TipRegion(rowRect, tip + desc);
                        },
                        context: new PawnContext(pawn),
                        filters: [sourceFilter, compatFilter]
                    );
                    Find.WindowStack.Add(new Window_AddItemNew(t, filters: [sourceFilter, compatFilter], pawn));
                }, onHover: onHover,
                style: new Style { size = new Size<Dimension>(Dimension.Length(MaxButtonWidth), Dimension.AUTO) });
        });
    }
}