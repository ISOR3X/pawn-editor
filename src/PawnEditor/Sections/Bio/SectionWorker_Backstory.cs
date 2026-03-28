using HotSwap;
using PawnEditor.Table;
using PawnEditor.TaffySharp;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_Backstory(SectionDef def) : SectionWorker(def)
{
    private const float MaxButtonWidth = 160f;

    protected override void DoSectionContents(TaffyBuilder col, Pawn pawn)
    {
        string childhoodLabel = "Childhood".Translate();
        string adulthoodLabel = "Adulthood".Translate();

        col.Text("Backstory", color: ColoredText.TipSectionTitleColor);
        col.Div(new Style { gap = Taffy.Gap(GenUI.GapSmall, GenUI.GapSmall), flexWrap = FlexWrap.Wrap }, row =>
        {
            DoBackstoryItem(row, pawn, BackstorySlot.Childhood, childhoodLabel);
            DoBackstoryItem(row, pawn, BackstorySlot.Adulthood, adulthoodLabel);
        });
    }

    // REF: CharacterCardUtility.DoLeftSection
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

        row.Row(new Style { gap = Taffy.Gap(GenUI.GapLabel) }, row2 =>
        {
            row2.Text(label);
            row2.Button(buttonLabel, onClick: _ =>
                {
                    Find.WindowStack.Add(new Window_AddItem(
                        new DefTableWorker_Backstory(
                            TableDefOf.PawnEditor_DefTable_ThingDef,
                            () => DefDatabase<BackstoryDef>.AllDefs
                                .Where(td => td.slot == slot)
                                .Cast<Def>()
                                .ToList(),
                            pawn),
                        [
                            ("Content source", () => new DefTableFilter_ContentSource()),
                        ]
                    ));
                }, onHover: onHover,
                style: new Style { size = new Size<Dimension>(Dimension.Length(MaxButtonWidth), Dimension.AUTO) });
        });
    }
}