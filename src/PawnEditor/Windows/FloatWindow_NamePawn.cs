using System;
using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Layout;
using L = PawnEditor.Layout.FlexLayoutHelper;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;

[HotSwappable]
public class FloatWindow_NamePawn(Rect boundWidgetRect) : FloatWindow(boundWidgetRect)
{
    private static bool forceNoNick;
    private static bool keepLastName;

    private CultureDef? _selectedCulture;
    private Gender _selectedGender = Gender.Male;
    private XenotypeDef? _selectedXenotype;

    protected override FloatWindowAlignment Alignment => FloatWindowAlignment.BottomCenter;

    public override Vector2 InitialSize => new(500, 200);


    public override void DoWindowContents(Rect inRect)
    {
        var p = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        if (p == null) return;
        _selectedCulture ??= p.Faction?.ideos?.PrimaryCulture;

        var cultures = DefDatabase<CultureDef>.AllDefsListForReading;
        var xenotypes = DefDatabase<XenotypeDef>.AllDefsListForReading;

        var layout = L.Row([
            L.Cell(rect =>
            {
                if (UIUtility.ButtonTextLabeled(rect, "Culture", _selectedCulture!.LabelCap))
                    Find.WindowStack.Add(new FloatMenu(cultures
                        .Select(c => new FloatMenuOption(c.LabelCap, () => _selectedCulture = c)).ToList()));
            }, flexBasis: 0.5f ).When(_selectedCulture != null),

            L.Cell(rect =>
            {
                if (UIUtility.ButtonTextLabeled(rect, "Xenotype", _selectedXenotype?.LabelCap ?? "None"))
                    Find.WindowStack.Add(new FloatMenu(xenotypes
                        .Select(x => new FloatMenuOption(x.LabelCap, () => _selectedXenotype = x))
                        .Append(new FloatMenuOption("None", () => _selectedXenotype = null)).ToList()));
            }, flexBasis: 0.5f).When(ModsConfig.BiotechActive),

            L.Cell(rect =>
            {
                if (UIUtility.ButtonTextLabeled(rect, "Gender", _selectedGender.GetLabel().CapitalizeFirst()))
                    Find.WindowStack.Add(new FloatMenu(
                        new List<Gender> { Gender.Male, Gender.Female }
                            .Select(g => new FloatMenuOption(g.GetLabel().CapitalizeFirst(), () => _selectedGender = g))
                            .ToList()));
            }, flexBasis: 0.5f),

            L.Cell(rect => Widgets.CheckboxLabeled(rect, "Keep last name", ref keepLastName), flexBasis: 0.5f),
            L.Cell(rect => Widgets.CheckboxLabeled(rect, "Force no nickname", ref forceNoNick), flexBasis: 0.5f),

            L.Row([
                L.Cell(rect =>
                {
                    using (new TextBlock(TextAnchor.MiddleLeft, ColoredText.SubtleGrayColor))
                        Widgets.Label(rect, p.Name.ToStringFull);
                }, flexBasis: 0.4f),
                L.Cell(rect =>
                {
                    if (Widgets.ButtonText(rect, "Generate"))
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        string? lastName = null;
                        if (keepLastName && p.Name is NameTriple triple) lastName = triple.Last;
                        p.Name = PawnBioAndNameGenerator.GenerateFullPawnName(p.def,
                            p.kindDef.GetNameMaker(p.gender), p.story,
                            _selectedXenotype, p.RaceProps.GetNameGenerator(_selectedGender),
                            _selectedCulture, p.IsCreepJoiner, _selectedGender,
                            p.RaceProps.nameCategory, lastName, forceNoNick);
                    }
                }, flexBasis: 0.5f, flexGrow:1)
            ], flexBasis: 1f)
        ], gapX: 24f, wrap: true);

        var height = FlexLayoutEngine.Draw(layout, inRect, (action, rect) =>
        {
            action(rect);
            return UIUtility.ButtonHeight;
        });

        if (!Mathf.Approximately(windowRect.height, height))
            windowRect.height = height + Margin * 2;
    }

    private static LayoutNode<Func<Rect, float>>
        CreateCell(Action<Rect> draw, float flexGrow = 0f, float flexBasis = 0.5f,
            float height = UIUtility.ButtonHeight) => new()
    {
        flexBasis = flexBasis,
        flexGrow = flexGrow,
        leaf = rect =>
        {
            draw(rect);
            return height;
        }
    };
}