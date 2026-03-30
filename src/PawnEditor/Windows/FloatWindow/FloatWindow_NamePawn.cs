using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using PawnEditor.TaffySharp;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;

[HotSwappable]
public class FloatWindow_NamePawn(Rect boundWidgetRect, Pawn pawn) : FloatWindow(boundWidgetRect)
{
    private static bool _forceNoNick;
    private static bool _keepLastName;

    private CultureDef? _selectedCulture;
    private Gender _selectedGender = Gender.Male;
    private XenotypeDef? _selectedXenotype;

    protected override Window? Owner => Find.WindowStack.WindowOfType<Window_Editor>();

    protected override FloatWindowAlignment Alignment => FloatWindowAlignment.BottomCenter;

    public override Vector2 InitialSize => new(500, 200);

    public override void DoWindowContents(Rect inRect)
    {
        _selectedCulture ??= pawn.Faction?.ideos?.PrimaryCulture;

        var cultures = DefDatabase<CultureDef>.AllDefsListForReading;
        var xenotypes = DefDatabase<XenotypeDef>.AllDefsListForReading;

        // MeasuredGrid runs with unconstrained height so Taffy computes the exact content height,
        // which we use to auto-resize the window below.
        var contentHeight = Taffy.MeasuredGrid(inRect,
            columns: [Taffy.Fr(), Taffy.Fr(2), Taffy.Fr(), Taffy.Fr(2)],
            gapX: GenUI.GapLabel, gapY: GenUI.GapSmall, autoRowHeight: UIUtility.ButtonHeight,
            build: grid =>
            {
                if (_selectedCulture != null)
                {
                    grid.Text("Culture");
                    grid.Button(_selectedCulture.LabelCap, block: true, onClick: _ =>
                    {
                        Find.WindowStack.Add(new FloatMenu(cultures
                            .Select(c => new FloatMenuOption(c.LabelCap, () => _selectedCulture = c))
                            .ToList()));
                    });
                }

                if (ModsConfig.BiotechActive)
                {
                    grid.Text("Xenotype");
                    grid.Button(_selectedXenotype?.LabelCap ?? "None", block: true, onClick: r =>
                    {
                        Find.WindowStack.Add(new FloatMenu(xenotypes
                            .Select(x => new FloatMenuOption(x.LabelCap, () => _selectedXenotype = x))
                            .Append(new FloatMenuOption("None", () => _selectedXenotype = null))
                            .ToList()));
                    });
                }

                grid.Text("Gender");
                grid.Button(_selectedGender.GetLabel().CapitalizeFirst(), block: true, onClick: r =>
                {
                    Find.WindowStack.Add(new FloatMenu(
                        new List<Gender> { Gender.Male, Gender.Female }
                            .Select(g =>
                                new FloatMenuOption(g.GetLabel().CapitalizeFirst(), () => _selectedGender = g))
                            .ToList()));
                });
                grid.GridItem(colSpan: 2,
                    draw: r => Verse.Widgets.CheckboxLabeled(r, "Keep last name", ref _keepLastName));
                grid.GridItem(colSpan: 2,
                    draw: r => Verse.Widgets.CheckboxLabeled(r, "Force no nickname", ref _forceNoNick));

                grid.Text(pawn.Name.ToStringFull, color: ColoredText.SubtleGrayColor,
                    style: new Style
                        { gridColumn = new Line<GridPlacement>(GridPlacement.Line(1), GridPlacement.Span(2)) });
                grid.Button(label: "Generate", block: true, onClick: _ =>
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        string? lastName = null;
                        if (_keepLastName && pawn.Name is NameTriple triple) lastName = triple.Last;
                        pawn.Name = PawnBioAndNameGenerator.GenerateFullPawnName(pawn.def,
                            pawn.kindDef.GetNameMaker(pawn.gender), pawn.story,
                            _selectedXenotype, pawn.RaceProps.GetNameGenerator(_selectedGender),
                            _selectedCulture, pawn.IsCreepJoiner, _selectedGender,
                            pawn.RaceProps.nameCategory, lastName, _forceNoNick);
                    },
                    style: new Style
                        { gridColumn = new Line<GridPlacement>(GridPlacement.Line(3), GridPlacement.Span(2)) });
            });

        if (!Mathf.Approximately(windowRect.height, contentHeight + Margin * 2))
            windowRect.height = contentHeight + Margin * 2;
    }
}