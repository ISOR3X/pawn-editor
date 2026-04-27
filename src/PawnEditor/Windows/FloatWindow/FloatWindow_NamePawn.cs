using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Verse.Sound;
using Void;
using Void.Components;
using Display = Taffy.Display;

namespace PawnEditor;

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
        var contentHeight = Void.Taffy.DivMeasured(inRect,
            b =>
            {
                if (_selectedCulture != null)
                {
                    b.Text("Culture");
                    GridButton(b, _selectedCulture.LabelCap,
                        _ =>
                        {
                            Find.WindowStack.Add(new FloatMenu(cultures
                                .Select(c => new FloatMenuOption(c.LabelCap, () => _selectedCulture = c))
                                .ToList()));
                        });
                }

                if (ModsConfig.BiotechActive)
                {
                    b.Text("Xenotype");
                    GridButton(b, _selectedXenotype?.LabelCap ?? "None",
                        r =>
                        {
                            Find.WindowStack.Add(new FloatMenu(xenotypes
                                .Select(x => new FloatMenuOption(x.LabelCap, () => _selectedXenotype = x))
                                .Append(new FloatMenuOption("None", () => _selectedXenotype = null))
                                .ToList()));
                        });
                }

                b.Text("Gender");
                GridButton(b, _selectedGender.GetLabel().CapitalizeFirst(),
                    r =>
                    {
                        Find.WindowStack.Add(new FloatMenu(
                            new List<Gender> { Gender.Male, Gender.Female }
                                .Select(g =>
                                    new FloatMenuOption(g.GetLabel().CapitalizeFirst(), () => _selectedGender = g))
                                .ToList()));
                    });
                b.GridItem(colSpan: 2,
                    draw: r => Verse.Widgets.CheckboxLabeled(r, "Keep last name", ref _keepLastName));
                b.GridItem(colSpan: 2,
                    draw: r => Verse.Widgets.CheckboxLabeled(r, "Force no nickname", ref _forceNoNick));

                b.Text(pawn.Name.ToStringFull, color: ColoredText.SubtleGrayColor,
                    style: new StyleOverride
                        { gridColumn = new Line<GridPlacement>(GridPlacement.Line(1), GridPlacement.Span(2)) });
                GridButton(b, "Generate", _ =>
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
            },
            new StyleOverride
            {
                gridTemplateColumns = [Void.Taffy.Fr(), Void.Taffy.Fr(2), Void.Taffy.Fr(), Void.Taffy.Fr(2)],
                display = Display.Grid,
                gap = Void.Taffy.Gap(GenUI.GapLabel, GenUI.GapTiny),
                gridAutoRows = [TrackSizingFunction.Px(UIUtility.ButtonHeight)]
            }
        );

        if (!Mathf.Approximately(windowRect.height, contentHeight + Margin * 2))
            windowRect.height = contentHeight + Margin * 2;
    }
}