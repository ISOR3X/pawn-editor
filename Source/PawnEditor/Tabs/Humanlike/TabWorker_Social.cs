using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class TabWorker_Social : TabWorker_Table<Pawn>
{
    private Vector2 scrollPos;

    public override void DrawTabContents(Rect rect, Pawn pawn)
    {
        var headerRect = rect.TakeTopPart(170);
        var portraitRect = headerRect.TakeLeftPart(170);
        headerRect = headerRect.ContractedBy(8f, 0f);
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(headerRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)), "TabLog".Translate().Colorize(ColoredText.TipSectionTitleColor));
        headerRect.xMin += 4f;
        headerRect.yMin += 4f;
        InteractionCardUtility.DrawInteractionsLog(headerRect, pawn, Find.PlayLog.AllEntries, 12);
        PawnEditor.DrawPawnPortrait(portraitRect);
        DoBottomOptions(rect.TakeBottomPart(UIUtility.RegularButtonHeight), pawn);
        DoRelations(rect.ContractedBy(4f), pawn);
    }

    private void DoBottomOptions(Rect inRect, Pawn pawn)
    {
        if (UIUtility.DefaultButtonText(ref inRect, "PawnEditor.QuickActions".Translate(), 80f))
            Find.WindowStack.Add(new FloatMenu(new()
            {
                new("PawnEditor.ForceRomance".Translate(), () =>
                {
                    static void DoRomance(Pawn initiator, Pawn recipient)
                    {
                        initiator.relations.TryRemoveDirectRelation(PawnRelationDefOf.ExLover, recipient);
                        initiator.relations.AddDirectRelation(PawnRelationDefOf.Lover, recipient);
                        TaleRecorder.RecordTale(TaleDefOf.BecameLover, initiator, recipient);
                    }

                    var pawns = PawnEditor.Pregame
                        ? Find.GameInitData.startingAndOptionalPawns
                        : pawn.MapHeld?.mapPawns.AllPawns
                       ?? pawn.GetCaravan()?.PawnsListForReading ?? PawnsFinder
                             .AllCaravansAndTravellingTransporters_Alive;

                    Find.WindowStack.Add(new FloatMenu(pawns.Where(p => p.RaceProps.Humanlike)
                       .Select(p => new FloatMenuOption(p.LabelCap, () =>
                       {
                           DoRomance(pawn, p);
                           table.ClearCache();
                       }))
                       .ToList()));
                    // ToDo: Add success message
                })
                // ToDo: Create baby from parent?
            }));
        inRect.xMin += 4f;

        if (UIUtility.DefaultButtonText(ref inRect, "PawnEditor.AddRelation".Translate()))
        {
            PawnEditor.AllPawns.UpdateCache(null, PawnCategory.Humans);
            var list = PawnEditor.AllPawns.GetList();
            list.Remove(pawn);
            Find.WindowStack.Add(new ListingMenu_Pawns(list, pawn, "Next".Translate(), p =>
            {
                Find.WindowStack.Add(new ListingMenu_Relations(pawn, p, table));
                return true;
            }));
        }

        inRect.xMin += 4f;
        var oldShowAll = SocialCardUtility.showAllRelations;
        Widgets.CheckboxLabeled(inRect, "PawnEditor.ShowAll.Relations".Translate(), ref SocialCardUtility.showAllRelations,
            placeCheckboxNearText: true);
        if (oldShowAll != SocialCardUtility.showAllRelations) table.ClearCache();
    }

    private void DoRelations(Rect inRect, Pawn pawn)
    {
        using (new TextBlock(TextAnchor.MiddleLeft))
            Widgets.Label(inRect.TakeTopPart(Text.LineHeightOf(GameFont.Small)), "Relations".Translate().Colorize(ColoredText.TipSectionTitleColor));
        inRect.xMin += 4f;
        var viewRect = new Rect(0, 0, inRect.width - 20, SocialCardUtility.cachedEntries.Count * 30 + Text.LineHeightOf(GameFont.Medium));
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        table.OnGUI(viewRect, pawn);
        Widgets.EndScrollView();
    }

    protected override List<UITable<Pawn>.Heading> GetHeadings() =>
        new()
        {
            new(35), // Icon
            new("PawnEditor.Relation".Translate(), 140, TextAnchor.LowerLeft),
            new("PawnEditor.Pawn".Translate(), textAnchor: TextAnchor.LowerLeft),
            new("Faction".Translate(), textAnchor: TextAnchor.LowerLeft),
            new("PawnEditor.Opinion".Translate().CapitalizeFirst(), 100),
            new(20),
            new(100), // Edit
            new(30), // Jump to pawn
            new(30) // Delete
        };

    protected override List<UITable<Pawn>.Row> GetRows(Pawn pawn)
    {
        SocialCardUtility.Recache(pawn);
        var result = new List<UITable<Pawn>.Row>(SocialCardUtility.cachedEntries.Count);
        for (var i = 0; i < SocialCardUtility.cachedEntries.Count; i++)
        {
            var items = new List<UITable<Pawn>.Row.Item>(5);
            var entry = SocialCardUtility.cachedEntries[i];
            items.Add(new(PawnEditor.GetPawnTex(entry.otherPawn, new(25, 25), Rot4.South, cameraZoom: 2f)));
            items.Add(new(SocialCardUtility.GetRelationsString(entry, pawn).Colorize(ColoredText.SubtleGrayColor), textAnchor: TextAnchor.MiddleLeft));
            items.Add(new(SocialCardUtility.GetPawnLabel(entry.otherPawn), i, TextAnchor.MiddleLeft));
            if (entry.otherPawn.Faction != Faction.OfPlayer)
                items.Add(new($"{entry.otherPawn.Faction.PlayerRelationKind.ToString()}, {entry.otherPawn.Faction.Name}", ColoredText.SubtleGrayColor,
                    textAnchor: TextAnchor.MiddleLeft));
            else
                items.Add(new());

            items.Add(new(opinionRect =>
            {
                opinionRect.xMin += 15;
                opinionRect.xMax -= 15;
                var opinionOf = entry.otherPawn.relations.OpinionOf(pawn);
                var opinionFrom = pawn.relations.OpinionOf(entry.otherPawn);
                using (new TextBlock(TextAnchor.MiddleLeft))
                    Widgets.Label(opinionRect,
                        opinionOf.ToStringWithSign().Colorize(opinionOf < 0 ? ColorLibrary.RedReadable : opinionOf > 0 ? ColorLibrary.Green : Color.white));
                using (new TextBlock(TextAnchor.MiddleRight))
                    Widgets.Label(opinionRect,
                        $"({opinionFrom.ToStringWithSign()})".Colorize((opinionFrom < 0 ? ColorLibrary.RedReadable :
                            opinionFrom > 0 ? ColorLibrary.Green : Color.white).FadedColor(0.8f)));
            }, entry.otherPawn.relations.OpinionOf(pawn)));
            items.Add(new());
            items.Add(new(editRect => EditUtility.EditButton(editRect, entry, pawn, table)));
            items.Add(new(TexPawnEditor.GoToPawn, () => PawnEditor.Select(entry.otherPawn)));
            if (entry.relations.Any(relation => !relation.implied))
                items.Add(new(TexButton.Delete, () =>
                {
                    foreach (var relation in entry.relations.Where(static relation => !relation.implied))
                        pawn.relations.TryRemoveDirectRelation(relation, entry.otherPawn);
                    table.ClearCache();
                }));
            else
                items.Add(new());


            result.Add(new(items, SocialCardUtility.GetPawnRowTooltip(entry, pawn)));
        }


        return result;
    }
}