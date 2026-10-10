using RimWorld;
using UnityEngine;
using Verse;
using Void.Native;
using static Void.Native.Components;
using UIBranch = Void.Taffy.UIBranch;

namespace PawnEditor.v2;

public class SectionWorker_Backstory : SectionWorker<Pawn>
{
    private static readonly Style SlotStyle = new() { display = TaffyDisplay.Block };

    /// <summary>
    ///     Only pawns with a story have backstories. Animals and mechs, for one, do not.
    /// </summary>
    public override bool ShowFor(object obj)
    {
        return base.ShowFor(obj) && obj is Pawn { story: not null };
    }

    public override void DoSectionContents(UIBranch b, Pawn pawn)
    {
        DoBackstoryItem(b, pawn, BackstorySlot.Childhood);
        DoBackstoryItem(b, pawn, BackstorySlot.Adulthood);
    }

    private static void DoBackstoryItem(UIBranch b, Pawn pawn, BackstorySlot slot)
    {
        var backstory = pawn.story.GetBackstory(slot);
        var buttonLabel = backstory != null ? backstory.TitleCapFor(pawn.gender) : "None".Translate().ToString();

        Action<Rect>? onHover = null;
        if (backstory != null)
            onHover = r => TooltipHandler.TipRegion(r, DescribeBackstory(backstory, pawn));

        b.Button(buttonLabel, onHover: onHover, disabled: backstory == null,
            onClick: _ => OpenTable(pawn, slot));
    }

    /// <summary>
    ///     The title and the description, as shown in a tooltip.
    /// </summary>
    private static string DescribeBackstory(BackstoryDef backstory, Pawn pawn)
    {
        var title = backstory.TitleCapFor(pawn.gender).Colorize(ColoredText.TipSectionTitleColor);
        return title + "\n\n" + backstory.FullDescriptionFor(pawn).Resolve();
    }

    private static void OpenTable(Pawn pawn, BackstorySlot slot)
    {
        Find.WindowStack.Add(new Window_Table<BackstoryDef>(
            [.. DefDatabase<BackstoryDef>.AllDefs.Where(def => def.slot == slot)],
            GetColumns(pawn),
            Find.WindowStack.WindowOfType<Window_Editor>(),
            GetFilters(),
            def => def.TitleCapFor(pawn.gender),
            def => SetBackstory(pawn, slot, def),
            (b, selected) =>
            {
                var current = slot == BackstorySlot.Adulthood ? pawn.story.Adulthood : pawn.story.Childhood;
                var currentTitle = current?.TitleCapFor(pawn.gender) ?? "None";
                var newTitle = selected != null
                    ? selected.TitleCapFor(pawn.gender)
                    : "None".Colorize(ColoredText.SubtleGrayColor);

                b.Div(slotBuilder =>
                {
                    slotBuilder.Text($"Current: {currentTitle}".Colorize(ColoredText.SubtleGrayColor));
                    slotBuilder.Text($"New: {newTitle}");
                }, style: SlotStyle);
            },
            (rect, def) => TooltipHandler.TipRegion(rect, DescribeBackstory(def, pawn))));
    }

    private static IReadOnlyList<RowFilter<BackstoryDef>> GetFilters()
    {
        return [new RowFilter_DefContentSource<BackstoryDef>(), new RowFilter_SpawnCategory()];
    }

    private static IReadOnlyList<TableColumn<BackstoryDef>> GetColumns(Pawn pawn)
    {
        return
        [
            TableColumn<BackstoryDef>.Text("Title", def => def.TitleCapFor(pawn.gender),
                comparison: StringComparison.CurrentCultureIgnoreCase),
            TableColumn<BackstoryDef>.Text("Source", def => def.modContentPack?.Name ?? "",
                TableColumnWidth.Fixed(150f), ColoredText.SubtleGrayColor),
            TableColumn<BackstoryDef>.Text("Spawn categories", def => string.Join(", ", def.spawnCategories),
                color: ColoredText.SubtleGrayColor)
        ];
    }

    private static void SetBackstory(Pawn pawn, BackstorySlot slot, BackstoryDef backstory)
    {
        if (slot == BackstorySlot.Adulthood) pawn.story.Adulthood = backstory;
        else pawn.story.Childhood = backstory;

        // Backstories can disable work types and skills.
        pawn.Notify_DisabledWorkTypesChanged();
        pawn.skills?.Notify_SkillDisablesChanged();
    }
}
