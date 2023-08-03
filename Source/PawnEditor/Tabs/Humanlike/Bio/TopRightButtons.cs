using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public partial class TabWorker_Bio_Humanlike
{
    private void DoButtons(Rect buttonsRect, Pawn pawn)
    {
        using var block = new TextBlock(TextAnchor.MiddleCenter);
        Widgets.DrawHighlight(buttonsRect);
        buttonsRect = buttonsRect.ContractedBy(6);
        if (Widgets.ButtonText(buttonsRect.TakeBottomPart(25), "PawnEditor.EditAppearance".Translate())) { }

        var devStageRect = buttonsRect.TopHalf().LeftHalf().ContractedBy(2);
        var text = pawn.DevelopmentalStage.ToString().Translate().CapitalizeFirst();
        if (Mouse.IsOver(devStageRect))
        {
            Widgets.DrawHighlight(devStageRect);
            if (Find.WindowStack.FloatMenu == null)
                TooltipHandler.TipRegion(devStageRect, text.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + "DevelopmentalAgeSelectionDesc".Translate());
        }

        Widgets.Label(devStageRect.BottomHalf(), text);
        if (Widgets.ButtonImageWithBG(devStageRect.TopHalf(), pawn.DevelopmentalStage.Icon().Texture, new Vector2(22f, 22f)))
        {
            var options = new List<FloatMenuOption>
            {
                new("Adult".Translate().CapitalizeFirst(), () => SetDevStage(pawn, DevelopmentalStage.Adult),
                    DevelopmentalStageExtensions.AdultTex.Texture, Color.white),
                new("Child".Translate().CapitalizeFirst(), () => SetDevStage(pawn, DevelopmentalStage.Child),
                    DevelopmentalStageExtensions.ChildTex.Texture, Color.white),
                new("Baby".Translate().CapitalizeFirst(), () => SetDevStage(pawn, DevelopmentalStage.Baby),
                    DevelopmentalStageExtensions.BabyTex.Texture, Color.white)
            };
            Find.WindowStack.Add(new FloatMenu(options));
        }

        var xenotypeRect = buttonsRect.TopHalf().RightHalf().ContractedBy(2);
        text = pawn.genes.XenotypeLabelCap;
        if (Mouse.IsOver(xenotypeRect))
        {
            Widgets.DrawHighlight(xenotypeRect);
            if (Find.WindowStack.FloatMenu == null)
                TooltipHandler.TipRegion(xenotypeRect, text.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + "XenotypeSelectionDesc".Translate());
        }

        Widgets.Label(xenotypeRect.BottomHalf(), text);

        if (Widgets.ButtonImageWithBG(xenotypeRect.TopHalf(), pawn.genes.XenotypeIcon, new Vector2(22f, 22f)))
        {
            var list = new List<FloatMenuOption>();
            foreach (var item in DefDatabase<XenotypeDef>.AllDefs.OrderBy(x => 0f - x.displayPriority))
            {
                var xenotype = item;
                list.Add(new FloatMenuOption(xenotype.LabelCap,
                    () => pawn.genes.SetXenotype(xenotype), xenotype.Icon, XenotypeDef.IconColor, MenuOptionPriority.Default,
                    r => TooltipHandler.TipRegion(r, xenotype.descriptionShort ?? xenotype.description), null, 24f,
                    r => Widgets.InfoCardButton(r.x, r.y + 3f, xenotype), extraPartRightJustified: true));
            }

            foreach (var customXenotype in CharacterCardUtility.CustomXenotypes)
            {
                var customInner = customXenotype;
                list.Add(new FloatMenuOption(customInner.name.CapitalizeFirst() + " (" + "Custom".Translate() + ")",
                    delegate
                    {
                        if (!pawn.IsBaseliner()) pawn.genes.SetXenotype(XenotypeDefOf.Baseliner);
                        pawn.genes.xenotypeName = customXenotype.name;
                        pawn.genes.iconDef = customXenotype.IconDef;
                        foreach (var geneDef in customXenotype.genes) pawn.genes.AddGene(geneDef, !customXenotype.inheritable);
                    }, customInner.IconDef.Icon, XenotypeDef.IconColor, MenuOptionPriority.Default, null, null, 24f, delegate(Rect r)
                    {
                        if (Widgets.ButtonImage(new Rect(r.x, r.y + (r.height - r.width) / 2f, r.width, r.width), TexButton.DeleteX, GUI.color))
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmDelete".Translate(customInner.name.CapitalizeFirst()), delegate
                            {
                                var path = GenFilePaths.AbsFilePathForXenotype(customInner.name);
                                if (File.Exists(path))
                                {
                                    File.Delete(path);
                                    CharacterCardUtility.cachedCustomXenotypes = null;
                                }
                            }, true));
                            return true;
                        }

                        return false;
                    }, extraPartRightJustified: true));
            }

            list.Add(new FloatMenuOption("XenotypeEditor".Translate() + "...",
                delegate { Find.WindowStack.Add(new Dialog_CreateXenotype(-1, delegate { CharacterCardUtility.cachedCustomXenotypes = null; })); }));

            Find.WindowStack.Add(new FloatMenu(list));
        }

        var sexRect = buttonsRect.BottomHalf().LeftHalf().ContractedBy(2);
        Widgets.DrawHighlightIfMouseover(sexRect);
        Widgets.Label(sexRect.BottomHalf(), "PawnEditor.Sex".Translate());
        if (Widgets.ButtonImageWithBG(sexRect.TopHalf(), pawn.gender.GetIcon(), new Vector2(22f, 22f)) && pawn.kindDef.fixedGender == null
         && pawn.RaceProps.hasGenders)
        {
            var list = new List<FloatMenuOption>
            {
                new("Female".Translate().CapitalizeFirst(), () => SetGender(pawn, Gender.Female), GenderUtility.FemaleIcon,
                    Color.white),
                new("Male".Translate().CapitalizeFirst(), () => SetGender(pawn, Gender.Male), GenderUtility.MaleIcon, Color.white)
            };

            Find.WindowStack.Add(new FloatMenu(list));
        }

        var bodyRect = buttonsRect.BottomHalf().RightHalf().ContractedBy(2);
        Widgets.DrawHighlightIfMouseover(bodyRect);
        Widgets.Label(bodyRect.BottomHalf(), "PawnEditor.Shape".Translate());
        if (Widgets.ButtonImageWithBG(bodyRect.TopHalf(), TexPawnEditor.BodyTypeIcons[pawn.story.bodyType],
                new Vector2(22f, 22f)))
            Find.WindowStack.Add(new FloatMenu(DefDatabase<BodyTypeDef>.AllDefs.Select(bodyType => new FloatMenuOption(bodyType.defName.CapitalizeFirst(), () =>
                {
                    pawn.story.bodyType = bodyType;
                    RecacheGraphics(pawn);
                }, TexPawnEditor.BodyTypeIcons[bodyType], Color.white))
               .ToList()));
    }

    private static void SetDevStage(Pawn pawn, DevelopmentalStage stage)
    {
        var lifeStage = pawn.RaceProps.lifeStageAges.FirstOrDefault(lifeStage => lifeStage.def.developmentalStage == stage);
        if (lifeStage != null)
        {
            var num = lifeStage.minAge;
            pawn.ageTracker.AgeBiologicalTicks = (long)(num * 3600000L);
        }
    }

    private static void SetGender(Pawn pawn, Gender gender)
    {
        pawn.gender = gender;
        if (pawn.story.bodyType == BodyTypeDefOf.Female && gender == Gender.Male) pawn.story.bodyType = BodyTypeDefOf.Male;
        if (pawn.story.bodyType == BodyTypeDefOf.Male && gender == Gender.Female) pawn.story.bodyType = BodyTypeDefOf.Female;
        RecacheGraphics(pawn);
    }

    private static void RecacheGraphics(Pawn pawn)
    {
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
            if (pawn.IsColonist) PortraitsCache.SetDirty(pawn);
        });
    }
}
