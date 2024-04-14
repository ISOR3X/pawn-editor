using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public partial class TabWorker_Bio_Humanlike
{
    private string ageBiologicalBuffer;
    private string ageChronologicalBuffer;
    private Pawn bufferForPawn;

    private void DoBasics(Rect inRect, Pawn pawn)
    {
        inRect.xMax -= 10;
        Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "PawnEditor.Basic".Translate().Colorize(ColoredText.TipSectionTitleColor));
        inRect.xMin += 5;
        var name = "PawnEditor.Name".Translate();
        var age = "PawnEditor.Age".Translate();
        var childhood = "Childhood".Translate();
        var adulthood = "Adulthood".Translate();
        var leftWidth = UIUtility.ColumnWidth(3, name, age, childhood, adulthood) + 32f;
        var nameRect = inRect.TakeTopPart(30);
        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(nameRect.TakeLeftPart(leftWidth), name);
        if (pawn.Name is NameTriple nameTriple)
        {
            var firstRect = new Rect(nameRect);
            firstRect.width *= 0.333f;
            var nickRect = new Rect(nameRect);
            nickRect.width *= 0.333f;
            nickRect.x += nickRect.width;
            var lastRect = new Rect(nameRect);
            lastRect.width *= 0.333f;
            lastRect.x += nickRect.width * 2f;
            var first = nameTriple.First;
            var nick = nameTriple.Nick;
            var last = nameTriple.Last;
            CharacterCardUtility.DoNameInputRect(firstRect, ref first, 12);
            if (nameTriple.Nick == nameTriple.First || nameTriple.Nick == nameTriple.Last) GUI.color = new(1f, 1f, 1f, 0.5f);
            CharacterCardUtility.DoNameInputRect(nickRect, ref nick, 16);
            GUI.color = Color.white;
            CharacterCardUtility.DoNameInputRect(lastRect, ref last, 12);
            if (nameTriple.First != first || nameTriple.Nick != nick || nameTriple.Last != last)
                pawn.Name = new NameTriple(first, string.IsNullOrEmpty(nick) ? first : nick, last);

            TooltipHandler.TipRegionByKey(firstRect, "FirstNameDesc");
            TooltipHandler.TipRegionByKey(nickRect, "ShortIdentifierDesc");
            TooltipHandler.TipRegionByKey(lastRect, "LastNameDesc");
        }
        else if (pawn.Name is NameSingle nameSingle)
        {
            var nameSingleName = nameSingle.Name;
            CharacterCardUtility.DoNameInputRect(nameRect, ref nameSingleName, 16);
            if (nameSingleName != nameSingle.Name)
                pawn.Name = new NameSingle(nameSingleName);

            TooltipHandler.TipRegionByKey(nameRect, "ShortIdentifierDesc");
        }
        else
            Widgets.Label(nameRect, pawn.NameFullColored);

        inRect.yMin += 3;
        var ageRect = inRect.TakeTopPart(50);
        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(ageRect.TakeLeftPart(leftWidth), age);
        var bio = ageRect.LeftPart(0.6f).LeftHalf();
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleCenter))
            Widgets.Label(bio.TakeTopPart(Text.LineHeight), "PawnEditor.Biological".Translate());
        var ageBio = pawn.ageTracker.AgeBiologicalYears;
        if (bufferForPawn == null || bufferForPawn != pawn)
        {
            ageBiologicalBuffer = null;
            ageChronologicalBuffer = null;
            bufferForPawn = pawn;
        }


        var minAge = 0;
        if (pawn.ageTracker.Adult)
            minAge = (int)pawn.ageTracker.CurLifeStageRace.minAge;
        UIUtility.IntField(bio, ref ageBio, minAge, int.MaxValue, ref ageBiologicalBuffer);

        if (ageBio != pawn.ageTracker.AgeBiologicalYears)
        {
            pawn.ageTracker.AgeBiologicalTicks = ageBio * 3600000L;
            PawnEditor.Notify_PointsUsed();
        }

        var chrono = ageRect.LeftPart(0.6f).RightHalf();
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleCenter))
            Widgets.Label(chrono.TakeTopPart(Text.LineHeight), "PawnEditor.Chronological".Translate());
        var ageChrono = pawn.ageTracker.AgeChronologicalYears;

        UIUtility.IntField(chrono, ref ageChrono, 0, int.MaxValue, ref ageChronologicalBuffer);

        if (ageChrono != pawn.ageTracker.AgeChronologicalYears)
        {
            pawn.ageTracker.AgeChronologicalTicks = ageChrono * 3600000L;
            PawnEditor.Notify_PointsUsed();
        }


        if (pawn.story.Childhood != null)
        {
            inRect.yMin += 3;
            var childRect = inRect.TakeTopPart(30);
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(childRect.TakeLeftPart(leftWidth), childhood);
            if (Widgets.ButtonText(childRect.LeftPart(0.6f), pawn.story.Childhood.TitleCapFor(pawn.gender)))
                Find.WindowStack.Add(new ListingMenu_Backstories(pawn, BackstorySlot.Childhood));

            TooltipHandler.TipRegion(childRect.LeftPart(0.6f), (TipSignal)pawn.story.childhood.FullDescriptionFor(pawn).Resolve());
        }

        if (pawn.story.Adulthood != null)
        {
            inRect.yMin += 3;
            var adultRect = inRect.TakeTopPart(30);
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(adultRect.TakeLeftPart(leftWidth), adulthood);
            if (Widgets.ButtonText(adultRect.LeftPart(0.6f), pawn.story.Adulthood.TitleCapFor(pawn.gender)))
                Find.WindowStack.Add(new ListingMenu_Backstories(pawn, BackstorySlot.Adulthood));

            TooltipHandler.TipRegion(adultRect.LeftPart(0.6f), (TipSignal)pawn.story.adulthood.FullDescriptionFor(pawn).Resolve());
        }
    }
}
