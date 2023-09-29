using System;
using System.Collections.Generic;
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
                break;
            case (EndogeneCategory)0xC:
                return gene.exclusionTags.NotNullAndContains("Tail");
                break;
            case (EndogeneCategory)0xD:
                return gene.exclusionTags.NotNullAndContains("HairStyle");
                break;
            case (EndogeneCategory)0xE:
                return gene.exclusionTags.NotNullAndContains("EyeColor");
                break;
            case (EndogeneCategory)0xF:
                return gene.exclusionTags.NotNullAndContains("BeardStyle");
                break;
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
            EndogeneCategory.Nose => BodyPartDefOf.Nose.LabelCap,
            EndogeneCategory.Jaw => BodyPartDefOf.Jaw.LabelCap,
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

        DrawBottomButtons(inRect.TakeBottomPart(50));
        DoLeftSection(inRect.TakeLeftPart(170).ContractedBy(6, 0));

        mainTabs.Clear();
        mainTabs.Add(new("PawnEditor.Shape".Translate(), () => mainTab = MainTab.Shape, mainTab == MainTab.Shape));
        mainTabs.Add(new("Hair".Translate().CapitalizeFirst(), () => mainTab = MainTab.Hair, mainTab == MainTab.Hair));
        mainTabs.Add(new("Tattoos".Translate(), () => mainTab = MainTab.Tattoos, mainTab == MainTab.Tattoos));
        if (ModsConfig.BiotechActive)
            mainTabs.Add(new("Xenotype".Translate(), () => mainTab = MainTab.Xenotype, mainTab == MainTab.Xenotype));

        Widgets.DrawMenuSection(inRect);
        TabDrawer.DrawTabs(inRect, mainTabs);
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
                        DoIconOptions(inRect.ContractedBy(5), DefDatabase<BodyTypeDef>.AllDefsListForReading, def =>
                            {
                                pawn.story.bodyType = def;
                                TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                            }, def => TexPawnEditor.BodyTypeIcons[def], def => pawn.story.bodyType == def, 1, new[] { pawn.story.SkinColorBase },
                            (color, i) => pawn.story.SkinColorBase = color, ColorType.Misc, DefDatabase<ColorDef>.AllDefs.Select(def => def.color).ToList());
                        break;
                    case ShapeTab.Head:
                        DoIconOptions(inRect.ContractedBy(5), DefDatabase<HeadTypeDef>.AllDefsListForReading, def =>
                            {
                                pawn.story.headType = def;
                                TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                            }, def => def.GetGraphic(pawn.story.SkinColor, false, pawn.story.SkinColorOverriden).MatSouth.mainTexture,
                            def => pawn.story.headType == def, 1, new[] { pawn.story.SkinColorBase },
                            (color, i) => pawn.story.SkinColorBase = color, ColorType.Misc, DefDatabase<ColorDef>.AllDefs.Select(def => def.color).ToList());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;
            case MainTab.Hair:
                DoIconOptions(inRect.ContractedBy(5), DefDatabase<HairDef>.AllDefsListForReading, def =>
                    {
                        pawn.story.hairDef = def;
                        TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                    }, def => def.Icon,
                    def => pawn.story.hairDef == def, 1, new[] { pawn.story.HairColor },
                    (color, i) => pawn.story.HairColor = color, ColorType.Hair,
                    DefDatabase<ColorDef>.AllDefs.Where(def => def.colorType == ColorType.Hair).Select(def => def.color).ToList());
                break;
            case MainTab.Tattoos:
                shapeTabs.Clear();
                shapeTabs.Add(new("PawnEditor.Body".Translate(), () => shapeTab = ShapeTab.Body, shapeTab == ShapeTab.Body));
                shapeTabs.Add(new("PawnEditor.Head".Translate().CapitalizeFirst(), () => shapeTab = ShapeTab.Head, shapeTab == ShapeTab.Head));
                Widgets.DrawMenuSection(inRect);
                TabDrawer.DrawTabs(inRect, shapeTabs);
                switch (shapeTab)
                {
                    case ShapeTab.Body:
                        DoIconOptions(inRect.ContractedBy(5), DefDatabase<TattooDef>.AllDefsListForReading, def =>
                            {
                                pawn.style.BodyTattoo = def;
                                TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                            }, def => def.Icon,
                            def => pawn.style.BodyTattoo == def, 0, Array.Empty<Color>(), null, ColorType.Misc, null);
                        break;
                    case ShapeTab.Head:
                        DoIconOptions(inRect.ContractedBy(5), DefDatabase<TattooDef>.AllDefsListForReading, def =>
                            {
                                pawn.style.FaceTattoo = def;
                                TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                            }, def => def.Icon,
                            def => pawn.style.FaceTattoo == def, 0, Array.Empty<Color>(), null, ColorType.Misc, null);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;
            case MainTab.Xenotype:
                DoXenotypeOptions(inRect.ContractedBy(5));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void DoIconOptions<T>(Rect inRect, List<T> options, Action<T> onSelected, Func<T, Texture> getIcon, Func<T, bool> isSelected, int colorCount,
        Color[] colors, Action<Color, int> setColor, ColorType colorType, List<Color> availableColors)
    {
        if (selectedColorIndex + 1 > colorCount) selectedColorIndex = 0;
        if (colorCount > 0)
        {
            var rect = new Rect(inRect.xMax - 26, inRect.yMax - 26, 18, 18);
            if (Widgets.ButtonImage(rect, TexPawnEditor.OpenColorPicker))
                Find.WindowStack.Add(new Dialog_ColorPicker(color => setColor(color, selectedColorIndex), colorType, colors[selectedColorIndex]));

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
            var rect = new Rect(i % itemsPerRow * itemSize, Mathf.Floor((float)i / itemsPerRow) * itemSize, itemSize, itemSize).ContractedBy(2);
            Widgets.DrawHighlight(rect);
            if (isSelected(option)) Widgets.DrawBox(rect);
            if (Widgets.ButtonInvisible(rect)) onSelected(option);

            GUI.DrawTexture(rect.ContractedBy(4), getIcon(option));
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
            Widgets.DrawHighlight(rect);
            if (pawn.genes.HasGene(option)) Widgets.DrawBox(rect);
            if (Widgets.ButtonInvisible(rect))
            {
                if (pawn.genes.HasGene(option))
                    pawn.genes.RemoveGene(pawn.genes.GetGene(option));
                else
                    foreach (var geneDef in options)
                    {
                        if (pawn.genes.GetGene(geneDef) is { } gene) pawn.genes.RemoveGene(gene);

                        pawn.genes.AddGene(option, false);
                    }
            }

            GUI.color = option.IconColor;
            GUI.DrawTexture(rect.ContractedBy(4), option.Icon);
            GUI.color = Color.white;

            TooltipHandler.TipRegion(rect, option.LabelCap);
        }

        Widgets.EndGroup();
    }

    private void DoLeftSection(Rect inRect)
    {
        PawnEditor.DrawPawnPortrait(inRect.TakeTopPart(170));
        var buttonsRect = inRect.TakeTopPart(140);
        Widgets.DrawHighlight(buttonsRect);
        buttonsRect = buttonsRect.ContractedBy(6);
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
                new("Adult".Translate().CapitalizeFirst(), () => TabWorker_Bio_Humanlike.SetDevStage(pawn, DevelopmentalStage.Adult),
                    DevelopmentalStageExtensions.AdultTex.Texture, Color.white),
                new("Child".Translate().CapitalizeFirst(), () => TabWorker_Bio_Humanlike.SetDevStage(pawn, DevelopmentalStage.Child),
                    DevelopmentalStageExtensions.ChildTex.Texture, Color.white),
                new("Baby".Translate().CapitalizeFirst(), () => TabWorker_Bio_Humanlike.SetDevStage(pawn, DevelopmentalStage.Baby),
                    DevelopmentalStageExtensions.BabyTex.Texture, Color.white)
            };
            Find.WindowStack.Add(new FloatMenu(options));
        }

        var sexRect = buttonsRect.BottomHalf().LeftHalf().ContractedBy(2);
        Widgets.DrawHighlightIfMouseover(sexRect);
        Widgets.Label(sexRect.BottomHalf(), "PawnEditor.Sex".Translate());
        if (Widgets.ButtonImageWithBG(sexRect.TopHalf(), pawn.gender.GetIcon(), new Vector2(22f, 22f)) && pawn.kindDef.fixedGender == null
         && pawn.RaceProps.hasGenders)
        {
            var list = new List<FloatMenuOption>
            {
                new("Female".Translate().CapitalizeFirst(), () => TabWorker_Bio_Humanlike.SetGender(pawn, Gender.Female), GenderUtility.FemaleIcon,
                    Color.white),
                new("Male".Translate().CapitalizeFirst(), () => TabWorker_Bio_Humanlike.SetGender(pawn, Gender.Male), GenderUtility.MaleIcon, Color.white)
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
                    TabWorker_Bio_Humanlike.RecacheGraphics(pawn);
                }, TexPawnEditor.BodyTypeIcons[bodyType], Color.white))
               .ToList()));

        inRect.yMin += 2;

        using (new TextBlock(GameFont.Tiny)) Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "DominantStyle".Translate().CapitalizeFirst());

        if (Widgets.ButtonText(inRect.TakeTopPart(30).ContractedBy(3), "Default".Translate()))
            Messages.Message("PawnEditor.NoStyles".Translate(), MessageTypeDefOf.RejectInput, false);


        using (new TextBlock(GameFont.Tiny)) Widgets.Label(inRect.TakeTopPart(Text.LineHeight), "Source".Translate().CapitalizeFirst());

        if (Widgets.ButtonText(inRect.TakeTopPart(30).ContractedBy(3), sourceFilter?.Name ?? "All".Translate()))
            Find.WindowStack.Add(new FloatMenu(LoadedModManager.RunningMods.Select(mod => new FloatMenuOption(mod.Name, () => sourceFilter = mod))
               .Prepend(new(
                    "All".Translate(), () => sourceFilter = null))
               .ToList()));

        if (ModsConfig.BiotechActive)
            Widgets.CheckboxLabeled(inRect.TakeBottomPart(50), "PawnEditor.IgnoreXenotype".Translate(), ref ignoreXenotype);
        Widgets.CheckboxLabeled(inRect.TakeBottomPart(30), "ShowApparel".Translate(), ref PawnEditor.RenderClothes);
        Widgets.CheckboxLabeled(inRect.TakeBottomPart(30), "ShowHeadgear".Translate(), ref PawnEditor.RenderHeadgear);
    }

    private void DrawBottomButtons(Rect inRect)
    {
        if (Widgets.ButtonText(inRect.TakeLeftPart(210).ContractedBy(5), "PawnEditor.GotoGearTab".Translate()))
        {
            Close();
            PawnEditor.Select(pawn);
            PawnEditor.GotoTab(PawnEditorDefOf.Gear);
        }

        if (Widgets.ButtonText(inRect.TakeRightPart(210).ContractedBy(5), "Accept".Translate())) OnAcceptKeyPressed();

        if (Widgets.ButtonText(new Rect(0, 0, 200, 40).CenteredOnXIn(inRect).CenteredOnYIn(inRect), "Randomize".Translate()))
            Find.WindowStack.Add(new FloatMenu(new()
            {
                new("Randomize".Translate() + " " + "PawnEditor.Shape".Translate(), () => TabWorker_Bio_Humanlike.RandomizeShape(pawn)),
                new("Randomize".Translate() + " " + "PawnEditor.Head".Translate().CapitalizeFirst(), () =>
                {
                    pawn.story.TryGetRandomHeadFromSet(from x in DefDatabase<HeadTypeDef>.AllDefs
                        where x.randomChosen
                        select x);
                    pawn.drawer.renderer.graphics.SetAllGraphicsDirty();
                    PortraitsCache.SetDirty(pawn);
                }),
                new("Randomize".Translate() + " " + "Tattoos".Translate(), () =>
                {
                    pawn.style.FaceTattoo = DefDatabase<TattooDef>.AllDefsListForReading.RandomElement();
                    pawn.style.BodyTattoo = DefDatabase<TattooDef>.AllDefsListForReading.RandomElement();
                    pawn.drawer.renderer.graphics.SetAllGraphicsDirty();
                    PortraitsCache.SetDirty(pawn);
                })
            }));
    }

    private enum MainTab
    {
        Shape,
        Hair,
        Tattoos,
        Xenotype
    }

    private enum ShapeTab
    {
        Body, Head
    }
}
