// using HotSwap;
// using PawnEditor.Extensions;
// using Verse;
//
// namespace PawnEditor;
//
// [HotSwappable]
// public class SectionWorker_BioName(SectionDef def) : SectionWorker(def)
// {
//     protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
//     {
//         if (pawn.Faction == null) return;
//
//         var isHuman = PawnUtility.GetPawnCategory(pawn) is PawnUtility.PawnCategory.Humanlike;
//         var nameRect = listing.RectLabeled("Name");
//         BioUtility.DoNameInputRect(nameRect, pawn, isHuman);
//     }
// }