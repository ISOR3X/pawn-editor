using PawnEditor.Table;
using PawnEditor.Table.Filters.BackstoryDef;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;
using Col = PawnEditor.Table.ColumnWorker<RimWorld.BackstoryDef>;
using Display = Taffy.Display;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_Backstory(SectionDef def) : SectionWorker(def)
{
    public override void OnLayout(Layout layout, Pawn pawn)
    {
        foreach (var slot in (List<string>)["childhood", "adulthood"])
        {
            var backstorySlot = slot == "childhood" ? BackstorySlot.Childhood : BackstorySlot.Adulthood;
            var btn = layout.ComponentById<ButtonElement>($"button_{slot}");

            DoBackstoryItem(btn, pawn, backstorySlot);
        }
    }

    private static void DoBackstoryItem(ButtonElement button, Pawn pawn, BackstorySlot slot)
    {
        var backstory = pawn.story.GetBackstory(slot);
        var buttonLabel = backstory != null ? backstory.TitleCapFor(pawn.gender) : "None".Translate().ToString();

        Action<Rect>? onHover = null;
        if (backstory != null)
            onHover = r =>
            {
                var tip = buttonLabel.Colorize(ColoredText.TipSectionTitleColor) + "\n\n";
                var desc = backstory.FullDescriptionFor(pawn).Resolve();
                TooltipHandler.TipRegion(r, tip + desc);
            };

        button.Label = buttonLabel;
        button.OnClick = _ =>
        {
            if (backstory != null)
                Find.WindowStack.Add(new Window_Table<BackstoryDef>(GetBackstoryTable(pawn, slot),
                    Find.WindowStack.WindowOfType<Window_Editor>(),
                    selectedItemSlot: (b, i) =>
                    {
                        b.Div(b2 =>
                        {
                            var newBackstory = i != null
                                ? i.TitleCapFor(pawn.gender)
                                : "None".Colorize(ColoredText.SubtleGrayColor);
                            var currentBackstory = slot == BackstorySlot.Adulthood
                                ? pawn.story.adulthood
                                : pawn.story.Childhood;
                            b2.Text($"Current: {currentBackstory.TitleCapFor(pawn.gender)}".Colorize(ColoredText
                                .SubtleGrayColor));
                            b2.Text($"New: {newBackstory}");
                        }, new StyleOverride { display = Display.Block });
                    }));
            else Messages.Message($"This pawn can not have an {slot} story.", MessageTypeDefOf.RejectInput);
        };
        button.OnHover = onHover;
        button.Disabled = backstory == null;
    }

    private static Table<BackstoryDef> GetBackstoryTable(Pawn pawn, BackstorySlot slot)
    {
        return new Table<BackstoryDef>(
            DefDatabase<BackstoryDef>.AllDefs.Where(td => td.slot == slot),
            [
                Col.Create<PawnContext>(
                    Void.Taffy.Fr(),
                    (grid, def, ctx) => grid.Text(def.TitleCapFor(ctx.Value.gender)),
                    "Title",
                    (a, b) => string.Compare(
                        a.TitleCapFor(pawn.gender),
                        b.TitleCapFor(pawn.gender),
                        StringComparison.CurrentCultureIgnoreCase)
                ),
                Col.CreateText(
                    Void.Taffy.Px(150f),
                    def => def.modContentPack?.Name ?? "",
                    "Source",
                    ColoredText.SubtleGrayColor
                ),
                Col.CreateText(
                    Void.Taffy.Fr(),
                    def => string.Join(", ", def.spawnCategories),
                    "Spawn categories",
                    ColoredText.SubtleGrayColor
                )
            ],
            onRowHover: (rowRect, rowBackstory, ctx) =>
            {
                if (ctx is not PawnContext pawnCtx) return;
                var tip = rowBackstory.TitleCapFor(pawnCtx.Value.gender)
                    .Colorize(ColoredText.TipSectionTitleColor) + "\n\n";
                var desc = rowBackstory.FullDescriptionFor(pawn).Resolve();
                TooltipHandler.TipRegion(rowRect, tip + desc);
            },
            context: new PawnContext(pawn),
            filters: [new RowFilter_DefContentSource<BackstoryDef>(), new RowFilter_SpawnCategory()],
            searchProjection: def => def.TitleCapFor(pawn.gender)
        );
    }
}