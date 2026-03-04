using System;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_BioBackstory(SectionDef def) : SectionWorker(def)
{
    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        listing.LabelH2("Backstory");

        DoBackstoryRect(listing, pawn);
    }


    // REF: CharacterCardUtility.DoLeftSection
    private static void DoBackstoryRect(Listing_Standard listing, Pawn pawn)
    {
        string childhoodLabel = "Childhood".Translate();
        string adulthoodLabel = "Adulthood".Translate();
        var labelWidth = Mathf.Max(childhoodLabel.GetWidthCached(), adulthoodLabel.GetWidthCached());
        labelWidth += UIUtility.LabelPadding * 3f;

        var slots = Enum.GetValues(typeof(BackstorySlot));
        for (var i = 0; i < slots.Length; i++)
        {

            var backstorySlot = (BackstorySlot)slots.GetValue(i);
            var backstory = pawn.story.GetBackstory(backstorySlot);
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                var rect1 = listing.GetRect(UIUtility.ButtonHeight);
                Widgets.Label(rect1.TakeLeftPart(labelWidth),
                    backstorySlot == BackstorySlot.Adulthood ? adulthoodLabel : childhoodLabel);
                if (backstory == null)
                {
                    using (new TextBlock(TextAnchor.MiddleCenter, ColoredText.SubtleGrayColor))
                        Widgets.Label(rect1, "None".Translate());
                    continue;
                }

                var backstoryLabel = backstory.TitleCapFor(pawn.gender);
                Widgets.ButtonText(rect1, backstoryLabel.Truncate(rect1.width - UIUtility.ButtonPadding));

                if (Mouse.IsOver(rect1))
                {
                    var tip = backstoryLabel.Colorize(ColoredText.TipSectionTitleColor) + "\n\n";
                    var desc = backstory.FullDescriptionFor(pawn).Resolve();
                    TooltipHandler.TipRegion(rect1, tip + desc);
                }
                if (i < slots.Length - 1) listing.Gap(2f);
            }
            
        }
    }
}