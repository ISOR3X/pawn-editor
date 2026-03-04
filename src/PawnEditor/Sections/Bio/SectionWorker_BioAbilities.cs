// using System.Collections.Generic;
// using System.Linq;
// using HotSwap;
// using PawnEditor.Extensions;
// using RimWorld;
// using UnityEngine;
// using Verse;
//
// namespace PawnEditor;
//
// [HotSwappable]
// public class SectionWorker_BioAbilities(SectionDef def) : SectionWorker(def)
// {
//     public const float AbilitiesHeight = 36f;
//     
//     protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
//     {
//         listing.LabelH2("Abilities");
//         
//         var abilityRect = listing.GetRect(GetAbilitiesHeight(pawn, listing.ColumnWidth));
//         DoAbilitiesRect(abilityRect, pawn);
//         listing.Gap(listing.verticalSpacing);
//         
//         listing.ButtonText_Fit("Add ability");
//     }
//     private static float GetAbilitiesHeight(Pawn pawn, float width)
//     {
//         var abilities = GetAbilities(pawn);
//         return UIUtility.DrawElementStackSectionHeight(abilities, _ => AbilitiesHeight, width, AbilitiesHeight);
//         
//     }
//
//     private static void DoAbilitiesRect(Rect inRect, Pawn pawn)
//     {   
//         UIUtility.DrawElementStackSection(inRect, GetAbilities(pawn),
//             (r, abil) =>
//             {
//                 GUI.DrawTexture(r, BaseContent.ClearTex);
//                 if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);
//                 if (Widgets.ButtonImage(r, abil.def.uiIcon, false))
//                 {
//                     if (Event.current.shift) TryDeleteAbility(abil.def, pawn);
//                     else Find.WindowStack.Add(new Dialog_InfoCard(abil.def));
//                 }
//                 if (Mouse.IsOver(r))
//                     TooltipHandler.TipRegion(r, new TipSignal(() =>
//                             abil.Tooltip + "\n\n" +
//                             "ClickToLearnMore".Translate().Colorize(ColoredText.SubtleGrayColor) +
//                             "\n" + "Shift + left click to delete.".Colorize(ColoredText.SubtleGrayColor),
//                         (int)r.y * 37));
//             },
//             _ => AbilitiesHeight,
//             rowHeight: AbilitiesHeight);
//     }
//     
//     private static List<Ability> GetAbilities(Pawn pawn) =>
//         pawn.abilities.AllAbilitiesForReading
//             .Where(a => a.def.showOnCharacterCard)
//             .OrderBy(a => a.def.level)
//             .ThenBy(a => a.def.EntropyGain)
//             .ToList();
//
//     
//     private static void TryDeleteAbility(AbilityDef abilityDef, Pawn pawn)
//     {
//         var ability = pawn.abilities.abilities.FirstOrDefault(x => x.def == abilityDef);
//         if (ability == null)
//         {
//             Messages.Message($"Failed to delete ability {abilityDef.defName} (it was not related to a def)",
//                 MessageTypeDefOf.RejectInput);
//             return;
//         }
//
//         pawn.abilities.RemoveAbility(abilityDef);
//     }
// }