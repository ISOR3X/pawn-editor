// using System.Collections.Generic;
// using System.Linq;
// using JetBrains.Annotations;
// using RimWorld;
// using UnityEngine;
// using Verse;
//
// namespace PawnEditor;
//
// [UsedImplicitly]
// [Reloadable]
// public class SectionWorker_AppearanceShape(SectionDef def) : SectionWorker(def)
// {
//     private Vector2 _scrollPositionBodyType = Vector2.zero;
//     private Vector2 _scrollPositionHeadType = Vector2.zero;
//     private float _skinColorHeight = 30f;
//
//     protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
//     {
//         var capturedPawn = pawn;
//
//         var rect1 = listing.GetRect(UIComponents.CarrouselCellHeight + UIUtility.ButtonHeight);
//         UIComponents.WidgetLabel(rect1.TakeTopPart(UIUtility.ButtonHeight), "Body");
//         UIComponents.Carrousel(rect1,
//             DefDatabase<BodyTypeDef>.AllDefsListForReading
//                 .Where(d => AppearanceUtility.CanUseBodyType(d, pawn)).ToList(),
//             ref _scrollPositionBodyType,
//             pawn.story.bodyType, d => AppearanceUtility.TrySetBodyType(d, capturedPawn),
//             d => AppearanceUtility.BodyTypes[d], pawn.story.SkinColor, d => d.defName);
//
//         var rect2 = listing.GetRect(UIComponents.CarrouselCellHeight + UIUtility.ButtonHeight);
//         UIComponents.WidgetLabel(rect2.TakeTopPart(UIUtility.ButtonHeight), "Head");
//         UIComponents.Carrousel(rect2,
//             DefDatabase<HeadTypeDef>.AllDefsListForReading
//                 .Where(d => AppearanceUtility.CanUseHeadType(d, pawn)).ToList(),
//             ref _scrollPositionHeadType,
//             pawn.story.headType, d => AppearanceUtility.SetHeadType(d, capturedPawn),
//             d => d.GetGraphic(capturedPawn, capturedPawn.story.SkinColor).MatSouth.mainTexture,
//             pawn.story.SkinColor, d => d.defName);
//
//         var skinColor = pawn.story.SkinColor;
//         var availableColors = AppearanceUtility.GetSkinColorsFor(pawn);
//         var specialColors = new Dictionary<string, Color> { { "Old", pawn.story.SkinColor } };
//         if (pawn.story.favoriteColor != null) specialColors["Favorite"] = pawn.story.favoriteColor.color;
//         if (pawn.story.SkinColorOverriden && pawn.story.skinColorBase != null)
//             specialColors["Base"] = pawn.story.skinColorBase.Value;
//         
//         listing.ColorPickerLabeled("Skin Color", _skinColorHeight, ref skinColor, specialColors, availableColors,
//             c => AppearanceUtility.TrySetSkinColor(c, ref capturedPawn), out _skinColorHeight);
//         AppearanceUtility.TrySetSkinColor(skinColor, ref pawn);
//     }
// }