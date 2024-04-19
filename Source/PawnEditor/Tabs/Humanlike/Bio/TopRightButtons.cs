using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimUI;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

public partial class TabWorker_Bio_Humanlike
{
    private Listing_Horizontal listing = new();
    private float height = 999f;
    private const float margin = 6f;

    public TabWorker_Bio_Humanlike()
    {
        listing.InlineSpacing = 4f;
        listing.BlockSpacing = 8f;
    }

    private void DoButtons(ref Rect buttonsRect, Pawn pawn)
    {
        using var block = new TextBlock(TextAnchor.MiddleCenter);
        var outerRect = buttonsRect.TakeTopPart(height + margin);
        Widgets.DrawHighlight(outerRect);
        buttonsRect = outerRect.ContractedBy(margin);
        listing.Begin(buttonsRect);
        string text;
        if (ModsConfig.BiotechActive)
        {
            text = pawn.DevelopmentalStage.ToString().Translate().CapitalizeFirst();
            // if (Widgets.ButtonImageWithBG(devStageRect.TakeTopPart(UIUtility.RegularButtonHeight), pawn.DevelopmentalStage.Icon().Texture, new Vector2(22f, 22f)))
            if (listing.ButtonImageLabeledVStack(text, pawn.DevelopmentalStage.Icon().Texture, 6, text.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + "DevelopmentalAgeSelectionDesc".Translate()))
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
        }

        if (ModsConfig.BiotechActive)
        {
            text = pawn.genes.XenotypeLabelCap;
            if (listing.ButtonImageLabeledVStack(text, pawn.genes.XenotypeIcon, 6, text.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + "XenotypeSelectionDesc".Translate()))
            {
                var list = new List<FloatMenuOption>();
                foreach (var item in DefDatabase<XenotypeDef>.AllDefs.OrderBy(x => 0f - x.displayPriority))
                {
                    if (HARCompat.Active && HARCompat.EnforceRestrictions && !HARCompat.CanUseXenotype(item, pawn))
                        continue;
                    var xenotype = item;
                    list.Add(new(xenotype.LabelCap,
                        () =>
                        {
                            SetXenotype(pawn, xenotype);
                            PawnEditor.Notify_PointsUsed();
                        }, xenotype.Icon, XenotypeDef.IconColor, MenuOptionPriority.Default,
                        r => TooltipHandler.TipRegion(r, xenotype.descriptionShort ?? xenotype.description), null, 24f,
                        r => Widgets.InfoCardButton(r.x, r.y + 3f, xenotype), extraPartRightJustified: true));
                }

                foreach (var customXenotype in CharacterCardUtility.CustomXenotypes)
                {
                    var customInner = customXenotype;
                    list.Add(new(customInner.name.CapitalizeFirst() + " (" + "Custom".Translate() + ")",
                        delegate
                        {
                            SetXenotype(pawn, customInner);
                            PawnEditor.Notify_PointsUsed();
                        }, customInner.IconDef.Icon, XenotypeDef.IconColor, MenuOptionPriority.Default, null, null, 24f, delegate(Rect r)
                        {
                            if (Widgets.ButtonImage(new(r.x, r.y + (r.height - r.width) / 2f, r.width, r.width), TexButton.Delete, GUI.color))
                            {
                                Find.WindowStack.Add(new Dialog_Confirm("ConfirmDelete".Translate(customInner.name.CapitalizeFirst()), "ConfirmDeleteXenotype",
                                    delegate
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

                list.Add(new("XenotypeEditor".Translate() + "...",
                    delegate
                    {
                        var index = PawnEditor.Pregame ? StartingPawnUtility.PawnIndex(pawn) : CharacterCardUtility.CustomXenotypes.Count;
                        Find.WindowStack.Add(new Dialog_CreateXenotype(index, delegate
                        {
                            CharacterCardUtility.cachedCustomXenotypes = null;
                            SetXenotype(pawn, StartingPawnUtility.GetGenerationRequest(index).ForcedCustomXenotype);
                        }));
                    }));

                Find.WindowStack.Add(new FloatMenu(list));
            }
        }

        if (listing.ButtonImageLabeledVStack("PawnEditor.Sex".Translate(), pawn.gender.GetIcon(), 6)
            && pawn.kindDef.fixedGender == null
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

        if (listing.ButtonImageLabeledVStack("PawnEditor.Shape".Translate(), TexPawnEditor.BodyTypeIcons[pawn.story.bodyType], 6))
            Find.WindowStack.Add(new FloatMenu(DefDatabase<BodyTypeDef>.AllDefs.Where(bodyType => pawn.DevelopmentalStage switch
                {
                    DevelopmentalStage.Baby or DevelopmentalStage.Newborn => bodyType == BodyTypeDefOf.Baby,
                    DevelopmentalStage.Child => bodyType == BodyTypeDefOf.Child,
                    DevelopmentalStage.Adult => bodyType != BodyTypeDefOf.Baby && bodyType != BodyTypeDefOf.Child,
                    _ => true
                })
                .Select(bodyType => new FloatMenuOption(bodyType.defName.CapitalizeFirst(), () =>
                {
                    pawn.story.bodyType = bodyType;
                    RecacheGraphics(pawn);
                }, TexPawnEditor.BodyTypeIcons[bodyType], Color.white))
                .ToList()));

        if (listing.ButtonText("PawnEditor.EditAppearance".Translate(), 6))
            Find.WindowStack.Add(new Dialog_AppearanceEditor(pawn));

        listing.End();
        height = listing.curHeight;
    }

    public static void SetDevStage(Pawn pawn, DevelopmentalStage stage)
    {
        var lifeStage = pawn.RaceProps.lifeStageAges.FirstOrDefault(lifeStage => lifeStage.def.developmentalStage == stage);
        var oldStage = pawn.DevelopmentalStage; // ageTracker changes this so we store the old stage before it is changed.

        if (lifeStage != null)
        {
            var num = lifeStage.minAge;
            pawn.ageTracker.AgeBiologicalTicks = (long)(num * 3600000L);
        }

        if (oldStage != stage)
        {
            pawn.apparel?.DropAllOrMoveAllToInventory(apparel => !apparel.def.apparel.developmentalStageFilter.Has(stage));
            var bodyTypeFor = PawnGenerator.GetBodyTypeFor(pawn);
            pawn.story.bodyType = bodyTypeFor;
            RecacheGraphics(pawn);
        }
    }

    public static void SetGender(Pawn pawn, Gender gender)
    {
        pawn.gender = gender;
        if (pawn.story.bodyType == BodyTypeDefOf.Female && gender == Gender.Male) pawn.story.bodyType = BodyTypeDefOf.Male;
        if (pawn.story.bodyType == BodyTypeDefOf.Male && gender == Gender.Female) pawn.story.bodyType = BodyTypeDefOf.Female;

        // HAR doesn't like head types not matching genders, so make sure to fix that
        if (HARCompat.Active && pawn.story.headType.gender != gender
                             && !pawn.story.TryGetRandomHeadFromSet(HARCompat.FilterHeadTypes(DefDatabase<HeadTypeDef>.AllDefs, pawn)))
            Log.Warning("Failed to find head type for " + pawn);

        RecacheGraphics(pawn);
    }

    public static void RecacheGraphics(Pawn pawn)
    {
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            pawn.drawer.renderer.SetAllGraphicsDirty();
            if (pawn.IsColonist) PortraitsCache.SetDirty(pawn);
        });
    }

    private static void ClearXenotype(Pawn pawn)
    {
        if (pawn.genes.xenotype != null)
            foreach (var xenotypeGene in pawn.genes.xenotype.genes)
            {
                var gene = (pawn.genes.xenotype.inheritable ? pawn.genes.Endogenes : pawn.genes.Xenogenes).FirstOrDefault(g => g.def == xenotypeGene);
                pawn.genes.RemoveGene(gene);
            }

        if (pawn.genes.CustomXenotype is { } customXenotype)
            foreach (var xenotypeGene in customXenotype.genes)
            {
                var gene = (customXenotype.inheritable ? pawn.genes.Endogenes : pawn.genes.Xenogenes).FirstOrDefault(g => g.def == xenotypeGene);
                pawn.genes.RemoveGene(gene);
            }
    }

    public static void SetXenotype(Pawn pawn, XenotypeDef xenotype)
    {
        ClearXenotype(pawn);
        foreach (var gene in xenotype.genes)
            pawn.genes.AddGene(gene, !xenotype.inheritable);

        pawn.genes.SetXenotypeDirect(xenotype);
    }

    public static void SetXenotype(Pawn pawn, CustomXenotype xenotype)
    {
        ClearXenotype(pawn);
        pawn.genes.xenotypeName = xenotype.name;
        pawn.genes.iconDef = xenotype.IconDef;
        foreach (var geneDef in xenotype.genes) pawn.genes.AddGene(geneDef, !xenotype.inheritable);
    }
}