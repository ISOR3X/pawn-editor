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
        PawnEditor.DrawPawnPortrait(portraitRect);
        DoBottomOptions(rect.TakeBottomPart(UIUtility.RegularButtonHeight), pawn);
        DoRelations(rect, pawn);
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
                              .AllCaravansAndTravelingTransportPods_Alive;

                    Find.WindowStack.Add(new FloatMenu(pawns.Where(p => p.RaceProps.Humanlike)
                        .Select(p => new FloatMenuOption(p.LabelCap, () => DoRomance(pawn, p)))
                        .ToList()));
                })
            }));
        inRect.xMin += 4f;
        
        if (UIUtility.DefaultButtonText(ref inRect, "PawnEditor.AddRelation".Translate()))
        {
        }

        inRect.xMin += 4f;
        var oldShowAll = SocialCardUtility.showAllRelations;
        Widgets.CheckboxLabeled(inRect, "PawnEditor.ShowAll.Relations".Translate(), ref SocialCardUtility.showAllRelations,
            placeCheckboxNearText: true);
        if (oldShowAll != SocialCardUtility.showAllRelations) table.ClearCache();
    }

    private void DoRelations(Rect inRect, Pawn pawn)
    {
        var viewRect = new Rect(0, 0, inRect.width - 20, SocialCardUtility.cachedEntries.Count * 30 + Text.LineHeightOf(GameFont.Medium));
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        table.OnGUI(viewRect, pawn);
        Widgets.EndScrollView();
    }

    protected override List<UITable<Pawn>.Heading> GetHeadings() =>
        new()
        {
            new("Relations".Translate(), 242),
            new("PawnEditor.Relation".Translate()),
            new("PawnEditor.Opinion".Translate().CapitalizeFirst(), 140),
            new(100),
            new(30)
        };

    protected override List<UITable<Pawn>.Row> GetRows(Pawn pawn)
    {
        SocialCardUtility.CheckRecache(pawn);
        var result = new List<UITable<Pawn>.Row>(SocialCardUtility.cachedEntries.Count);
        for (var i = 0; i < SocialCardUtility.cachedEntries.Count; i++)
        {
            var items = new List<UITable<Pawn>.Row.Item>(5);
            var entry = SocialCardUtility.cachedEntries[i];
            items.Add(new(SocialCardUtility.GetPawnLabel(entry.otherPawn), Widgets.PlaceholderIconTex, i));
            items.Add(new(SocialCardUtility.GetRelationsString(entry, pawn)));
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
            items.Add(new("Edit".Translate() + "...", () => { }));
            if (entry.relations.Any(relation => !relation.implied))
                items.Add(new(TexButton.DeleteX, () =>
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