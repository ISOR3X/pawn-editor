using HotSwap;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Verse.Sound;
using Void;
using Void.Components;

namespace PawnEditor;

[HotSwappable]
public class FloatWindow_NamePawn(Rect boundWidgetRect, Pawn pawn, Window? owner = null)
    : FloatWindow(boundWidgetRect, owner)
{
    private static bool _forceNoNick;
    private static bool _keepLastName;

    private CultureDef? _selectedCulture;
    private Gender _selectedGender = Gender.Male;
    private XenotypeDef? _selectedXenotype;

    protected override FloatWindowAlignment Alignment => FloatWindowAlignment.BottomCenter;

    public override Vector2 InitialSize => new(500, 200);

    private static void GridButton(TaffyBuilder grid, string label, Action<Rect>? onClick = null,
        StyleOverride? style = null)
    {
        style ??= new StyleOverride();
        style = style.Merge(new StyleOverride
        {
            width = Dimension.AUTO, justifySelf = AlignItems.Stretch
        });
        grid.Button(label, style: style, onClick: onClick);
    }

    public override void DoWindowContents(Rect inRect)
    {
        _selectedCulture ??= pawn.Faction?.ideos?.PrimaryCulture;

        var cultures = DefDatabase<CultureDef>.AllDefsListForReading;
        var xenotypes = DefDatabase<XenotypeDef>.AllDefsListForReading;

        // MeasuredGrid runs with unconstrained height, so Taffy computes the exact content height,
        // which we use to auto-resize the window below.
        var contentHeight = Void.Taffy.MeasuredGrid(inRect,
            [Void.Taffy.Fr(), Void.Taffy.Fr(2), Void.Taffy.Fr(), Void.Taffy.Fr(2)],
            GenUI.GapLabel, GenUI.GapTiny, UIUtility.ButtonHeight,
            grid =>
            {
                if (_selectedCulture != null)
                {
                    grid.Text("Culture");
                    GridButton(grid, _selectedCulture.LabelCap,
                        _ =>
                        {
                            Find.WindowStack.Add(new FloatMenu(cultures
                                .Select(c => new FloatMenuOption(c.LabelCap, () => _selectedCulture = c))
                                .ToList()));
                        });
                }

                if (ModsConfig.BiotechActive)
                {
                    grid.Text("Xenotype");
                    GridButton(grid, _selectedXenotype?.LabelCap ?? "None",
                        r =>
                        {
                            Find.WindowStack.Add(new FloatMenu(xenotypes
                                .Select(x => new FloatMenuOption(x.LabelCap, () => _selectedXenotype = x))
                                .Append(new FloatMenuOption("None", () => _selectedXenotype = null))
                                .ToList()));
                        });
                }

                grid.Text("Gender");
                GridButton(grid, _selectedGender.GetLabel().CapitalizeFirst(),
                    r =>
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
                    style: new StyleOverride
                        { gridColumn = new Line<GridPlacement>(GridPlacement.Line(1), GridPlacement.Span(2)) });
                GridButton(grid, "Generate", _ =>
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
                    new StyleOverride
                    {
                        gridColumn = new Line<GridPlacement>(GridPlacement.Line(3), GridPlacement.Span(2))
                    });
            });

        if (!Mathf.Approximately(windowRect.height, contentHeight + Margin * 2))
            windowRect.height = contentHeight + Margin * 2;
    }
}