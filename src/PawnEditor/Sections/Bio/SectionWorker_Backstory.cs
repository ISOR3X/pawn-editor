using HotSwap;
using PawnEditor.Table;
using Taffy;
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

    private static Table<BackstoryDef> GetBackstoryTable(Pawn pawn, BackstorySlot slot)
    {
        return new Table<BackstoryDef>(
            rows: DefDatabase<BackstoryDef>.AllDefs.Where(td => td.slot == slot),
            columns:
            [
                Col.Create<PawnContext>(
                    Taffy.Fr(),
                    (grid, def, ctx) => grid.Text(def.TitleCapFor(ctx.Value.gender)),
                    "Title",
                    compare: (a, b) => string.Compare(
                        a.TitleCapFor(pawn.gender),
                        b.TitleCapFor(pawn.gender),
                        StringComparison.CurrentCultureIgnoreCase)
                ),
                Col.CreateText(
                    Taffy.Px(150f),
                    def => def.modContentPack?.Name ?? "",
                    "Source",
                    color: ColoredText.SubtleGrayColor
                ),
                // TODO: FIXME - Table widths are recalculated every width? When the column below becomes wider when scrolled down (on many rows), the columns are also resized.
                // Headers do not seem to be influenced by this.
                Col.CreateText(
                    Taffy.Fr(),
                    def => string.Join(",", def.spawnCategories),
                    "Spawn categories",
                    color: ColoredText.SubtleGrayColor
                ),
            ],
            onRowHover: (rowRect, rowBackstory, ctx) =>
            {
                if (ctx is not ITableContext<Pawn> pawnCtx) return;
                var tip = rowBackstory.TitleCapFor(pawnCtx.Value.gender)
                    .Colorize(ColoredText.TipSectionTitleColor) + "\n\n";
                var desc = rowBackstory.FullDescriptionFor(pawn).Resolve();
                TooltipHandler.TipRegion(rowRect, tip + desc);
            },
            context: new PawnContext(pawn),
            filters: [new RowFilter_DefContentSource<BackstoryDef>(), new RowFilter_BackstoryDefSpawnCategory()],
            searchProjection: def => def.TitleCapFor(pawn.gender)
        );
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
            row2.Button(buttonLabel,
                onClick: _ =>
                {
                    Find.WindowStack.Add(new Window_Table<BackstoryDef>(GetBackstoryTable(pawn, slot), pawn,
                        Find.WindowStack.WindowOfType<Window_Editor>()));
                },
                onHover: onHover,
                style: new Style { size = new Size<Dimension>(Dimension.Length(MaxButtonWidth), Dimension.AUTO) });
        });
    }
}