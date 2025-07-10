using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[StaticConstructorOnStartup]
[HotSwappable]
public class Dialog_AppearanceEditor : Window
{
    private static readonly List<EndogeneCategory> geneCategories = new()
    {
        EndogeneCategory.BodyType,
        EndogeneCategory.Melanin,
        (EndogeneCategory)0xB,
        (EndogeneCategory)0xC,
        EndogeneCategory.Headbone,
        EndogeneCategory.Jaw,
        (EndogeneCategory)0xD,
        EndogeneCategory.HairColor,
        (EndogeneCategory)0xE,
        EndogeneCategory.Nose,
        EndogeneCategory.Voice,
        (EndogeneCategory)0xF,
        EndogeneCategory.Hands
    };

    private static readonly List<List<GeneDef>> genesByCategory;

    private readonly List<TabRecord> mainTabs = new(3);
    private readonly Pawn pawn;
    private readonly List<TabRecord> shapeTabs = new(2);
    private bool ignoreXenotype;

    private float lastColorHeight;

    private FloatMenuOption lastRandomization;

    private float lastXenotypeHeight;
    private MainTab mainTab;

    private Vector2 scrollPos;

    private int selectedColorIndex;
    private ShapeTab shapeTab;
    private ModContentPack sourceFilter;

    static Dialog_AppearanceEditor()
    {
        genesByCategory = Enumerable.Repeat(0, geneCategories.Count).Select(i => new List<GeneDef>(i)).ToList();
        foreach (var geneDef in DefDatabase<GeneDef>.AllDefsListForReading)
            for (var i = 0; i < geneCategories.Count; i++)
                if (InCategory(geneCategories[i], geneDef))
                    genesByCategory[i].Add(geneDef);
    }

    public Dialog_AppearanceEditor(Pawn pawn)
    {
        this.pawn = pawn;
        closeOnClickedOutside = false;
        doCloseX = false;
        doCloseButton = false;
        closeOnCancel = false;

        forcePause = true;
        absorbInputAroundWindow = true;
        closeOnAccept = false;
        closeOnCancel = true;
        forceCatchAcceptAndCancelEventEvenIfUnfocused = true;

        if (HARCompat.Active)
            HARCompat.Notify_AppearanceEditorOpen(pawn);
    }

    public override float Margin => 8;

    public override Vector2 InitialSize => new(1000, 700);

    private static bool InCategory(EndogeneCategory category, GeneDef gene)
    {
        if (category <= EndogeneCategory.Voice) return gene.endogeneCategory == category;
        switch (category)
        {
            case (EndogeneCategory)0xB:
                return gene.exclusionTags.NotNullAndContains("Fur");
            case (EndogeneCategory)0xC:
                return gene.exclusionTags.NotNullAndContains("Tail");
            case (EndogeneCategory)0xD:
                return gene.exclusionTags.NotNullAndContains("HairStyle");
            case (EndogeneCategory)0xE:
                return gene.exclusionTags.NotNullAndContains("EyeColor");
            case (EndogeneCategory)0xF:
                return gene.exclusionTags.NotNullAndContains("BeardStyle");
        }

        return false;
    }

    private static string GetLabel(EndogeneCategory category)
    {
        return category switch
        {
            EndogeneCategory.None => "None".Translate(),
            EndogeneCategory.Melanin => PawnSkinColors.SkinColorGenesInOrder[0].LabelCap,
            EndogeneCategory.HairColor => "HairColor".Translate().CapitalizeFirst(),
            EndogeneCategory.Ears => "PawnEditor.Ears".Translate(),
            EndogeneCategory.Nose => PawnEditorDefOf.Nose.LabelCap,
            EndogeneCategory.Jaw => PawnEditorDefOf.Jaw.LabelCap,
            EndogeneCategory.Hands => PawnEditorDefOf.Hands.LabelCap,
            EndogeneCategory.Headbone => "PawnEditor.Headbone".Translate(),
            EndogeneCategory.Head => BodyPartDefOf.Head.LabelCap,
            EndogeneCategory.BodyType => "PawnEditor.BodyType".Translate(),
            EndogeneCategory.Voice => "PawnEditor.Voice".Translate(),
            (EndogeneCategory)0xB => "PawnEditor.Fur".Translate(),
            (EndogeneCategory)0xC => PawnEditorDefOf.Tail.LabelCap,
            (EndogeneCategory)0xD => "Hair".Translate().CapitalizeFirst(),
            (EndogeneCategory)0xE => BodyPartGroupDefOf.Eyes.LabelCap,
            (EndogeneCategory)0xF => "Beard".Translate().CapitalizeFirst(),
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
    }

    public override void DoWindowContents(Rect inRect)
    {
        Widgets.BeginGroup(inRect);
        using (new TextBlock(GameFont.Medium))
        {
            var rect = inRect.TakeTopPart(Text.LineHeight * 2.5f);
            rect.y += Text.LineHeight / 4;
            using (new TextBlock(TextAnchor.UpperLeft))
                Widgets.Label(rect, "PawnEditor.EditAppearance".Translate());
            using (new TextBlock(TextAnchor.UpperRight))
            {
                Widgets.Label(rect, pawn.Name.ToStringShort + (", " + pawn.story.TitleCap).Colorize(ColoredText.SubtleGrayColor));
                var size = Text.CalcSize(pawn.Name.ToStringShort + ", " + pawn.story.TitleCap);
                GUI.DrawTexture(new(rect.xMax - size.x - rect.height * 0.6f, rect.y - Text.LineHeight / 4, rect.height * 0.6f, rect.height * 0.6f),
                    PawnEditor.GetPawnTex(pawn, new(rect.height, rect.height), Rot4.South));
            }
        }

        using (new TextBlock(GameFont.Small))
        {
            DrawBottomButtons(inRect.TakeBottomPart(50));
            DoLeftSection(inRect.TakeLeftPart(170).ContractedBy(6, 0));

            mainTabs.Clear();
            mainTabs.Add(new("PawnEditor.Shape".Translate(), () => mainTab = MainTab.Shape, mainTab == MainTab.Shape));
            mainTabs.Add(new("PawnEditor.Hair".Translate().CapitalizeFirst(), () => mainTab = MainTab.Hair, mainTab == MainTab.Hair));
            if (ModsConfig.IdeologyActive)
                mainTabs.Add(new("Tattoos".Translate(), () => mainTab = MainTab.Tattoos, mainTab == MainTab.Tattoos));
            if (ModsConfig.BiotechActive)
                mainTabs.Add(new("Xenotype".Translate(), () => mainTab = MainTab.Xenotype, mainTab == MainTab.Xenotype));
            if (HARCompat.Active)
                mainTabs.Add(new("HAR.RaceFeatures".Translate(), () => mainTab = MainTab.HAR, mainTab == MainTab.HAR));

            Widgets.DrawMenuSection(inRect);
            TabDrawer.DrawTabs(inRect, mainTabs, 400f);
            inRect.yMin += 40;

            switch (mainTab)
            {
                case MainTab.Shape:
                    shapeTabs.Clear();
                    shapeTabs.Add(new("PawnEditor.Body".Translate(), () => shapeTab = ShapeTab.Body, shapeTab == ShapeTab.Body));
                    shapeTabs.Add(new("PawnEditor.Head".Translate().CapitalizeFirst(), () => shapeTab = ShapeTab.Head, shapeTab == ShapeTab.Head));
                    Widgets.DrawMenuSection(inRect);
                    TabDrawer.DrawTabs(inRect, shapeTabs);
                    switch (shapeTab)
                    {
                        case ShapeTab.Body:
                            var bodyTypes = DefDatabase<BodyTypeDef>.AllDefs.Where(h => MatchesSource(h) && IsAllowed(h, pawn))
                                .Where(bodyType =>
                                    pawn.DevelopmentalStage switch
                                    {
                                        DevelopmentalStage.Baby or DevelopmentalStage.Newborn => bodyType == BodyTypeDefOf.Baby,
                                        DevelopmentalStage.Child => bodyType == BodyTypeDefOf.Child,
                                        DevelopmentalStage.Adult => bodyType != BodyTypeDefOf.Baby && bodyType != BodyTypeDefOf.Child,
                                        _ => true
                                    });

                            if (HARCompat.Active)
                            {
                                var allowedBodyTypes = HARCompat.AllowedBodyTypes(pawn);
                                if (!allowedBodyTypes.NullOrEmpty()) bodyTypes = bodyTypes.Intersect(allowedBodyTypes);
                            }

                            DoIconOptions(inRect.ContractedBy(5), bodyTypes
                                    .ToList(), def =>
                                {
                                    pawn.story.bodyType = def;
                                    TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                                }, def => TexPawnEditor.BodyTypeIcons[def], def => pawn.story.bodyType == def, 1, new[] { pawn.story.SkinColor },
                                (color, i) =>
                                {
                                    pawn.story.skinColorOverride = color;
                                    TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                                }, ColorType.Misc,
                                DefDatabase<ColorDef>.AllDefs.Select(static def => def.color).ToList());
                            break;
                        case ShapeTab.Head:
                            var headTypes = DefDatabase<HeadTypeDef>.AllDefs.Where(h => MatchesSource(h) && IsAllowed(h, pawn));
                            if (HARCompat.Active)
                            {
                                headTypes = HARCompat.FilterHeadTypes(headTypes, pawn);
                                // HAR doesn't like head types not matching genders
                                headTypes = headTypes.Where(type => type.gender == Gender.None || type.gender == pawn.gender);
                            }

                            DoIconOptions(inRect.ContractedBy(5), headTypes.ToList(), def =>
                                {
                                    pawn.story.headType = def;
                                    TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                                }, def => def.GetGraphic(pawn, pawn.story.HairColor).MatSouth.mainTexture,
                                def => pawn.story.headType == def, 1, new[] { pawn.story.SkinColor },
                                (color, i) =>
                                {
                                    pawn.story.skinColorOverride = color;
                                    TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                                }, ColorType.Misc,
                                DefDatabase<ColorDef>.AllDefs.Select(static def => def.color).ToList());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                case MainTab.Hair:
                    shapeTabs.Clear();
                    shapeTabs.Add(new("PawnEditor.Head".Translate().CapitalizeFirst(), () => shapeTab = ShapeTab.Head, shapeTab == ShapeTab.Head));
                    shapeTabs.Add(new("PawnEditor.Beard".Translate(), () => shapeTab = ShapeTab.Body, shapeTab == ShapeTab.Body));
                    Widgets.DrawMenuSection(inRect);
                    TabDrawer.DrawTabs(inRect, shapeTabs);
                    switch (shapeTab)
                    {
                        case ShapeTab.Head:
                            var hairTypes = DefDatabase<HairDef>.AllDefs.Where(MatchesSource);
                            if (HARCompat.Active && HARCompat.EnforceRestrictions) hairTypes = hairTypes.Where(hair => HARCompat.AllowStyleItem(hair, pawn));
                            DoIconOptions(inRect.ContractedBy(5), hairTypes.ToList(), def =>
                                {
                                    pawn.story.hairDef = def;
                                    TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                                }, def => def.Icon,
                                def => pawn.story.hairDef == def, 1, new[] { pawn.story.HairColor },
                                (color, i) =>
                                {
                                    pawn.story.HairColor = color;
                                    TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                                }, ColorType.Hair,
                                DefDatabase<ColorDef>.AllDefs.Where(static def => def.colorType == ColorType.Hair).Select(static def => def.color).ToList());
                            break;
                        case ShapeTab.Body:
                            var beardTypes = DefDatabase<BeardDef>.AllDefs.Where(MatchesSource);
                            if (HARCompat.Active && HARCompat.EnforceRestrictions) beardTypes = beardTypes.Where(hair => HARCompat.AllowStyleItem(hair, pawn));
                            DoIconOptions(inRect.ContractedBy(5), beardTypes.ToList(), def =>
                                {
                                    pawn.style.beardDef = def;
                                    TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                                }, def => def.Icon,
                                def => pawn.style.beardDef == def, 1, new[] { pawn.story.HairColor },
                                (color, i) =>
                                {
                                    pawn.story.HairColor = color;
                                    TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                                }, ColorType.Hair,
                                DefDatabase<ColorDef>.AllDefs.Where(static def => def.colorType == ColorType.Hair).Select(static def => def.color).ToList());
                            break;
                    }

                    break;
                case MainTab.Tattoos:
                    shapeTabs.Clear();
                    shapeTabs.Add(new("PawnEditor.Body".Translate(), () => shapeTab = ShapeTab.Body, shapeTab == ShapeTab.Body));
                    shapeTabs.Add(new("PawnEditor.Head".Translate().CapitalizeFirst(), () => shapeTab = ShapeTab.Head, shapeTab == ShapeTab.Head));
                    Widgets.DrawMenuSection(inRect);
                    TabDrawer.DrawTabs(inRect, shapeTabs);
                    var tattoos = DefDatabase<TattooDef>.AllDefs.Where(MatchesSource);
                    if (HARCompat.Active && HARCompat.EnforceRestrictions) tattoos = tattoos.Where(td => HARCompat.AllowStyleItem(td, pawn));
                    switch (shapeTab)
                    {
                        case ShapeTab.Body:
                            DoIconOptions(inRect.ContractedBy(5), tattoos.Where(static td => td.tattooType == TattooType.Body).ToList(), def =>
                                {
                                    pawn.style.BodyTattoo = def;
                                    TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                                }, static def => def.Icon,
                                def => pawn.style.BodyTattoo == def, 0, Array.Empty<Color>(), null, ColorType.Misc, null);
                            break;
                        case ShapeTab.Head:
                            DoIconOptions(inRect.ContractedBy(5),
                                tattoos.Where(static td => td.tattooType == TattooType.Face).ToList(), def =>
                                {
                                    pawn.style.FaceTattoo = def;
                                    TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                                }, static def => def.Icon,
                                def => pawn.style.FaceTattoo == def, 0, Array.Empty<Color>(), null, ColorType.Misc, null);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                case MainTab.Xenotype:
                    inRect.yMin -= TabDrawer.TabHeight;
                    DoXenotypeOptions(inRect.ContractedBy(5));
                    break;
                case MainTab.HAR:
                    HARCompat.DoRaceTabs(inRect.ContractedBy(5));
                    if (Event.current.type is EventType.MouseDown or EventType.Used)
                        TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        Widgets.EndGroup();
    }

    private void DoIconOptions<T>(Rect inRect, List<T> options, Action<T> onSelected, Func<T, Texture> getIcon, Func<T, bool> isSelected, int colorCount,
        Color[] colors, Action<Color, int> setColor, ColorType colorType, List<Color> availableColors)
    {
        if (selectedColorIndex + 1 > colorCount) selectedColorIndex = 0;
        if (colorCount > 0)
        {
            var rect = new Rect(inRect.xMax - 26, inRect.yMax - 26, 18, 18);
            if (Widgets.ButtonImage(rect, Designator_Eyedropper.EyeDropperTex))
                Find.WindowStack.Add(new Dialog_ColorPicker(color => setColor(color, selectedColorIndex), availableColors, colors[selectedColorIndex]));

            for (var i = 0; i < colorCount; i++)
            {
                rect.x -= 26;
                Widgets.DrawBoxSolid(rect, colors[i]);
                if (selectedColorIndex == i) Widgets.DrawBox(rect);
                if (Widgets.ButtonInvisible(rect)) selectedColorIndex = i;
            }

            var oldColor = colors[selectedColorIndex];
            Widgets.ColorSelector(inRect.TakeBottomPart(lastColorHeight + 10).ContractedBy(4), ref colors[selectedColorIndex], availableColors,
                out lastColorHeight, colorSize: 18);
            if (colors[selectedColorIndex] != oldColor) setColor(colors[selectedColorIndex], selectedColorIndex);
        }

        var itemsPerRow = 9;
        var itemSize = (inRect.width - 20) / itemsPerRow;
        while (itemSize > 192)
        {
            itemsPerRow++;
            itemSize = (inRect.width - 20) / itemsPerRow;
        }

        while (itemSize < 48)
        {
            itemsPerRow--;
            itemSize = (inRect.width - 20) / itemsPerRow;
        }

        var viewRect = new Rect(0, 0, inRect.width - 20, Mathf.Ceil((float)options.Count / itemsPerRow) * itemSize);
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var rect = new Rect(i % itemsPerRow * itemSize, Mathf.Floor((float)i / itemsPerRow) * itemSize, itemSize, itemSize).ContractedBy(6);
            Widgets.DrawHighlight(rect);

            if (option is Def def)
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawLightHighlight(rect);
                    var str = def.label ?? def.defName;
                    TooltipHandler.TipRegion(rect, str.CapitalizeFirst() + "\n\n" + "ModClickToSelect".Translate());
                }

            if (isSelected(option)) Widgets.DrawBox(rect);
            if (Widgets.ButtonInvisible(rect)) onSelected(option);

            GUI.DrawTexture(rect.ContractedBy(2), getIcon(option));
        }

        Widgets.EndScrollView();
    }

    private void DoXenotypeOptions(Rect inRect)
    {
        if (Event.current.type == EventType.Layout) lastXenotypeHeight = 9999;
        var viewRect = new Rect(0, 0, inRect.width - 20, lastXenotypeHeight);
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        for (var i = 0; i < geneCategories.Count; i++) DoGeneOptions(ref viewRect, GetLabel(geneCategories[i]), genesByCategory[i]);
        if (Event.current.type == EventType.Layout) lastXenotypeHeight -= viewRect.height;
        Widgets.EndScrollView();
    }

    private void DoGeneOptions(ref Rect inRect, string label, List<GeneDef> options)
    {
        Widgets.Label(inRect.TakeTopPart(Text.LineHeight), label);
        if (options.Count == 0)
        {
            Widgets.Label(inRect.TakeTopPart(Text.LineHeight).RightPart(0.9f), "PawnEditor.NoOptions".Translate().Colorize(ColoredText.SubtleGrayColor));
            return;
        }

        var itemsPerRow = 9;
        var itemSize = (inRect.width - 20) / itemsPerRow;
        while (itemSize > 192)
        {
            itemsPerRow++;
            itemSize = (inRect.width - 20) / itemsPerRow;
        }

        while (itemSize < 48)
        {
            itemsPerRow--;
            itemSize = (inRect.width - 20) / itemsPerRow;
        }

        Widgets.BeginGroup(inRect.TakeTopPart(Mathf.Ceil((float)options.Count / itemsPerRow) * itemSize));
        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var rect = new Rect(i % itemsPerRow * itemSize, Mathf.Floor((float)i / itemsPerRow) * itemSize, itemSize, itemSize).ContractedBy(2);
            bool enabled = GeneIsAllowed(option);
            Widgets.DrawHighlight(rect);
            if (pawn.genes.HasActiveGene(option))
            {
                Widgets.DrawBox(rect);
            }
            if (enabled && Widgets.ButtonInvisible(rect))
            {
                if (pawn.genes.HasActiveGene(option))
                {
                    pawn.genes.RemoveGene(pawn.genes.GetGene(option));
                }
                else
                {
                    foreach (var geneDef in options)
                    {
                        if (pawn.genes.GetGene(geneDef) is { } gene) pawn.genes.RemoveGene(gene);

                        pawn.genes.AddGene(option, false);
                    }
                }
            }

            GUI.color = enabled ? Color.white : Color.gray;
            // ToDo: Apply correct gene background texture according to gene category.
            GUI.DrawTexture(rect.ContractedBy(4), GeneUIUtility.GeneBackground_Endogene.Texture);
            GUI.color *= option.IconColor;
            GUI.DrawTexture(rect.ContractedBy(4), option.Icon);
            GUI.color = Color.white;

            TooltipHandler.TipRegion(rect, option.LabelCap + (enabled ? TaggedString.Empty : "\n\n" + "PawnEditor.XenotypeForbbiden".Translate()));
        }

        inRect.yMin += 8f;
        Widgets.EndGroup();
    }

    private bool GeneIsAllowed(GeneDef option)
    {
        if (ignoreXenotype) return true;
        if (HARCompat.Active && HARCompat.EnforceRestrictions && HARCompat.CanHaveGene(option, pawn) is false)
        {
            return false;
        }
        else if (pawn.IsBaseliner())
        {
            return option.endogeneCategory == EndogeneCategory.Melanin || option.endogeneCategory == EndogeneCategory.HairColor;
        }
        else if (pawn.genes.Xenotype != null)
        {
            return pawn.genes.Xenotype.AllGenes.Contains(option);
        }
        else if (pawn.genes.CustomXenotype != null)
        {
            return pawn.genes.CustomXenotype.genes.Contains(option);
        }

        return false;
    }

    private void DoLeftSection(Rect inRect)
    {
        inRect.yMin -= 30f;
        PawnEditor.DrawPawnPortrait(inRect.TakeTopPart(170));
        inRect.yMin += 8f;
        var buttonsRect = inRect.TakeTopPart(110);
        Widgets.DrawHighlight(buttonsRect);
        buttonsRect = buttonsRect.ContractedBy(4);

        using (new TextBlock(TextAnchor.MiddleCenter))
        {
            var sexRect = buttonsRect.TopHalf().LeftHalf().ContractedBy(2);
            Widgets.DrawHighlightIfMouseover(sexRect);

            if (Widgets.ButtonImageWithBG(sexRect.TakeTopPart(UIUtility.RegularButtonHeight), pawn.gender.GetIcon(), new Vector2(22f, 22f))
                && pawn.kindDef.fixedGender == null && pawn.RaceProps.hasGenders)
            {
                var list = new List<FloatMenuOption>
                {
                    new("Female".Translate().CapitalizeFirst(), () => TabWorker_Bio_Humanlike.SetGender(pawn, Gender.Female), GenderUtility.FemaleIcon,
                        Color.white),
                    new("Male".Translate().CapitalizeFirst(), () => TabWorker_Bio_Humanlike.SetGender(pawn, Gender.Male), GenderUtility.MaleIcon, Color.white)
                };

                Find.WindowStack.Add(new FloatMenu(list));
            }

            Widgets.Label(sexRect, "PawnEditor.Sex".Translate());

            TaggedString text;
            if (ModsConfig.BiotechActive)
            {
                var devStageRect = buttonsRect.TopHalf().RightHalf().ContractedBy(2);
                text = pawn.DevelopmentalStage.ToString().Translate().CapitalizeFirst();
                if (Mouse.IsOver(devStageRect))
                {
                    Widgets.DrawHighlight(devStageRect);
                    if (Find.WindowStack.FloatMenu == null)
                        TooltipHandler.TipRegion(devStageRect,
                            text.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + "DevelopmentalAgeSelectionDesc".Translate());
                }

                if (Widgets.ButtonImageWithBG(devStageRect.TakeTopPart(UIUtility.RegularButtonHeight), pawn.DevelopmentalStage.Icon().Texture,
                        new Vector2(22f, 22f)))
                {
                    var options = new List<FloatMenuOption>
                    {
                        new("Adult".Translate().CapitalizeFirst(), () => TabWorker_Bio_Humanlike.SetDevStage(pawn, DevelopmentalStage.Adult),
                            DevelopmentalStageExtensions.AdultTex.Texture, Color.white),
                        new("Child".Translate().CapitalizeFirst(), () => TabWorker_Bio_Humanlike.SetDevStage(pawn, DevelopmentalStage.Child),
                            DevelopmentalStageExtensions.ChildTex.Texture, Color.white),
                        new("Baby".Translate().CapitalizeFirst(), () => TabWorker_Bio_Humanlike.SetDevStage(pawn, DevelopmentalStage.Baby),
                            DevelopmentalStageExtensions.BabyTex.Texture, Color.white)
                    };
                    Find.WindowStack.Add(new FloatMenu(options));
                }

                Widgets.Label(devStageRect, text);

                var xenotypeRect = buttonsRect.BottomHalf().LeftHalf().ContractedBy(2);
                text = pawn.genes.XenotypeLabelCap;
                if (Mouse.IsOver(xenotypeRect))
                {
                    Widgets.DrawHighlight(xenotypeRect);
                    if (Find.WindowStack.FloatMenu == null)
                        TooltipHandler.TipRegion(xenotypeRect, text.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + "XenotypeSelectionDesc".Translate());
                }


                if (Widgets.ButtonImageWithBG(xenotypeRect.TakeTopPart(UIUtility.RegularButtonHeight), pawn.genes.XenotypeIcon, new Vector2(22f, 22f)))
                {
                    var list = new List<FloatMenuOption>();
                    foreach (var item in DefDatabase<XenotypeDef>.AllDefs.Where(x => x != pawn.genes.xenotype).OrderBy(x => 0f - x.displayPriority))
                    {
                        var xenotype = item;
                        list.Add(new(xenotype.LabelCap,
                            () => { SetXenotype(xenotype); }, xenotype.Icon, XenotypeDef.IconColor, MenuOptionPriority.Default,
                            r => TooltipHandler.TipRegion(r, xenotype.descriptionShort ?? xenotype.description), null, 24f,
                            r => Widgets.InfoCardButton(r.x, r.y + 3f, xenotype), extraPartRightJustified: true));
                    }

                    foreach (var customXenotype in CharacterCardUtility.CustomXenotypes.Where(x => x != pawn.genes.CustomXenotype))
                    {
                        var customInner = customXenotype;
                        list.Add(new(customInner.name.CapitalizeFirst() + " (" + "Custom".Translate() + ")",
                            delegate
                            {
                                if (!pawn.IsBaseliner()) pawn.genes.SetXenotype(XenotypeDefOf.Baseliner);
                                pawn.genes.xenotypeName = customXenotype.name;
                                pawn.genes.iconDef = customXenotype.IconDef;
                                foreach (var geneDef in customXenotype.genes) pawn.genes.AddGene(geneDef, !customXenotype.inheritable);
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
                        delegate { Find.WindowStack.Add(new Dialog_CreateXenotype(-1, delegate { CharacterCardUtility.cachedCustomXenotypes = null; })); }));

                    Find.WindowStack.Add(new FloatMenu(list));
                }

                Widgets.Label(xenotypeRect, text.Truncate(xenotypeRect.width));
            }
        }

        inRect.yMin += 6;

        /* Doesn't seem to be doing anything at the moment, disabled for now to avoid new bug reports
        using (new TextBlock(GameFont.Tiny)) Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "DominantStyle".Translate().CapitalizeFirst());

        if (Widgets.ButtonText(inRect.TakeTopPart(30).ContractedBy(3), "Default".Translate()))
            Messages.Message("PawnEditor.NoStyles".Translate(), MessageTypeDefOf.RejectInput, false);

        inRect.yMin += 4;

        */
        using (new TextBlock(GameFont.Tiny)) Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "Source".Translate().CapitalizeFirst());

        if (mainTab != MainTab.HAR && Widgets.ButtonText(inRect.TakeTopPart(30).ContractedBy(3), sourceFilter?.Name ?? "PawnEditor.All".Translate().CapitalizeFirst()))
        {
            var allDefs = GetAllDefsForTab(mainTab, shapeTab);
            var options = LoadedModManager.RunningMods.Intersect(allDefs.Select(def => def.modContentPack).Distinct())
                .Where(x => x != null)
                .Select(mod => new FloatMenuOption(mod.Name, () => sourceFilter = mod))
                .Prepend(new(
                    "PawnEditor.All".Translate().CapitalizeFirst(), () => sourceFilter = null))
                .ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }

        if (ModsConfig.BiotechActive)
            Widgets.CheckboxLabeled(inRect.TakeBottomPart(50), "PawnEditor.IgnoreXenotype".Translate(), ref ignoreXenotype);
        Widgets.CheckboxLabeled(inRect.TakeBottomPart(30), "PawnEditor.ShowApparel".Translate(), ref PawnEditor.RenderClothes);
        Widgets.CheckboxLabeled(inRect.TakeBottomPart(30), "PawnEditor.ShowHeadgear".Translate(), ref PawnEditor.RenderHeadgear);
    }

    private void SetXenotype(XenotypeDef xenotype)
    {
        for (int num = pawn.genes.endogenes.Count - 1; num >= 0; num--)
        {
            pawn.genes.RemoveGene(pawn.genes.endogenes[num]);
        }

        pawn.genes.ClearXenogenes();
        PawnGenerator.GenerateGenes(pawn, xenotype, default);
    }

    private void DrawBottomButtons(Rect inRect)
    {
        if (Widgets.ButtonText(inRect.TakeLeftPart(210).ContractedBy(5), "PawnEditor.GotoGearTab".Translate()))
        {
            Close();
            PawnEditor.Select(pawn);
            PawnEditor.GotoTab(PawnEditorDefOf.Gear);
        }

        if (Widgets.ButtonText(inRect.TakeRightPart(210).ContractedBy(5), "Accept".Translate()))
        {
            OnAcceptKeyPressed();
            Close();
        }

        var randomRect = new Rect(0, 0, 200, 40).CenteredOnXIn(inRect).CenteredOnYIn(inRect);

        if (lastRandomization != null && Widgets.ButtonImageWithBG(randomRect.TakeRightPart(20), TexUI.RotRightTex, new Vector2(12, 12)))
        {
            lastRandomization.action();
            randomRect.TakeRightPart(1);
        }

        if (Widgets.ButtonText(randomRect, "Randomize".Translate()))
        {
            var initialOptions = new List<FloatMenuOption>
            {
                new("PawnEditor.Shape".Translate(), () =>
                {
                    pawn.story.bodyType = (BodyTypeDef)GetAllDefsForTab(MainTab.Shape, ShapeTab.Body).Where(MatchesSource).RandomElement();
                    pawn.drawer.renderer.SetAllGraphicsDirty();
                    PortraitsCache.SetDirty(pawn);
                }),
                new("PawnEditor.Head".Translate().CapitalizeFirst(), () =>
                {
                    pawn.story.headType = (HeadTypeDef)GetAllDefsForTab(MainTab.Shape, ShapeTab.Head).Where(MatchesSource).RandomElement();
                    pawn.drawer.renderer.SetAllGraphicsDirty();
                    PortraitsCache.SetDirty(pawn);
                }),
            };

            if (ModsConfig.IdeologyActive)
            {
                initialOptions.Add(new("Tattoos".Translate(), () =>
                {
                    pawn.style.FaceTattoo = DefDatabase<TattooDef>.AllDefs.Where(MatchesSource).RandomElement();
                    pawn.style.BodyTattoo = DefDatabase<TattooDef>.AllDefs.Where(MatchesSource).RandomElement();
                    pawn.drawer.renderer.SetAllGraphicsDirty();
                    PortraitsCache.SetDirty(pawn);
                }));
            }

            var options = initialOptions.Select(opt => new FloatMenuOption("Randomize".Translate() + " " + opt.Label, () =>
                {
                    lastRandomization = opt;
                    opt.action();
                }))
                .ToList();

            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    private bool MatchesSource(Def def) => sourceFilter == null || def.modContentPack == sourceFilter;

    private bool IsAllowed(HeadTypeDef def, Pawn p)
    {
        if (ignoreXenotype) return true;
        if (ModsConfig.BiotechActive && !def.requiredGenes.NullOrEmpty())
        {
            if (p.genes == null)
            {
                return false;
            }

            foreach (GeneDef requiredGene in def.requiredGenes)
            {
                if (!pawn.genes.HasActiveGene(requiredGene))
                {
                    return false;
                }
            }
        }

        if (def.gender != 0)
        {
            return def.gender == p.gender;
        }

        return def.randomChosen;
    }

    private bool IsAllowed(BodyTypeDef def, Pawn p)
    {
        if (ignoreXenotype) return true;
        if (ModsConfig.BiotechActive && pawn.DevelopmentalStage.Juvenile())
        {
            return def == BodyTypeDefOf.Baby || def == BodyTypeDefOf.Child;
        }

        return true;
    }

    private IEnumerable<Def> GetAllDefsForTab(MainTab tab, ShapeTab shape)
    {
        switch (tab)
        {
            case MainTab.Shape:
                switch (shape)
                {
                    case ShapeTab.Body:
                        var bodyTypes = DefDatabase<BodyTypeDef>.AllDefs
                            .Where(bodyType =>
                                pawn.DevelopmentalStage switch
                                {
                                    DevelopmentalStage.Baby or DevelopmentalStage.Newborn => bodyType == BodyTypeDefOf.Baby,
                                    DevelopmentalStage.Child => bodyType == BodyTypeDefOf.Child,
                                    DevelopmentalStage.Adult => bodyType != BodyTypeDefOf.Baby && bodyType != BodyTypeDefOf.Child,
                                    _ => true
                                });

                        if (HARCompat.Active)
                        {
                            var allowedBodyTypes = HARCompat.AllowedBodyTypes(pawn);
                            if (!allowedBodyTypes.NullOrEmpty()) bodyTypes = bodyTypes.Intersect(allowedBodyTypes);
                        }

                        return bodyTypes;
                    case ShapeTab.Head:
                        var headTypes = DefDatabase<HeadTypeDef>.AllDefs;
                        if (HARCompat.Active)
                        {
                            headTypes = HARCompat.FilterHeadTypes(headTypes, pawn);
                            // HAR doesn't like head types not matching genders
                            headTypes = headTypes.Where(type => (type.gender == Gender.None || type.gender == pawn.gender) && type.randomChosen);
                        }

                        return headTypes;
                }

                break;
            case MainTab.Hair:
                return DefDatabase<HairDef>.AllDefsListForReading;
            case MainTab.Tattoos:
                return shape switch
                {
                    ShapeTab.Body => DefDatabase<TattooDef>.AllDefs.Where(td => td.tattooType == TattooType.Body).Cast<Def>().ToList(),
                    ShapeTab.Head => DefDatabase<TattooDef>.AllDefs.Where(td => td.tattooType == TattooType.Face).Cast<Def>().ToList(),
                    _ => new()
                };
            case MainTab.Xenotype:
                return genesByCategory.SelectMany(defs => defs.Cast<Def>());
        }

        return Enumerable.Empty<Def>();
    }

    private enum MainTab
    {
        Shape,
        Hair,
        Tattoos,
        Xenotype,
        HAR
    }

    private enum ShapeTab
    {
        Body,
        Head
    }
}