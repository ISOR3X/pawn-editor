using System.Collections.Generic;
using System.Linq;
using HotSwap;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnEditor;

[HotSwappable]
public class FloatWindow_NamePawn(Rect boundWidgetRect) : FloatWindow(boundWidgetRect)
{
    private static bool _forceNoNick;
    private static bool _keepLastName;

    private CultureDef? _selectedCulture;
    private Gender _selectedGender = Gender.Male;
    private XenotypeDef? _selectedXenotype;

    protected override Window? Owner => Find.WindowStack.WindowOfType<Window_Editor>();

    protected override FloatWindowAlignment Alignment => FloatWindowAlignment.BottomCenter;

    public override Vector2 InitialSize => new(500, 200);

    // Gap used between grid cells.
    private const float Gap = 12f;

    // Number of rows in the grid (always 4 regardless of conditions).
    private const int Rows = 4;


    public override void DoWindowContents(Rect inRect)
    {
        var p = Find.WindowStack.WindowOfType<Window_Editor>().GetSelectedPawn();
        if (p == null) return;
        _selectedCulture ??= p.Faction?.ideos?.PrimaryCulture;

        var cultures  = DefDatabase<CultureDef>.AllDefsListForReading;
        var xenotypes = DefDatabase<XenotypeDef>.AllDefsListForReading;

        // Four-column grid: [1fr, 2fr, 1fr, 2fr], rows sized to button height
        Taffy.Grid(inRect,
            columns: [Taffy.Fr(), Taffy.Fr(2), Taffy.Fr(), Taffy.Fr(2)],
            gap: Gap, autoRowHeight: UIUtility.ButtonHeight,
            build: grid =>
            {
                // ── Row 1: Culture + Xenotype ─────────────────────────────────
                if (_selectedCulture != null)
                {
                    grid.GridItem(draw: r => Verse.Widgets.Label(r, "Culture"));
                    grid.GridItem(draw: r =>
                    {
                        if (Verse.Widgets.ButtonText(r, _selectedCulture.LabelCap))
                            Find.WindowStack.Add(new FloatMenu(cultures
                                .Select(c => new FloatMenuOption(c.LabelCap, () => _selectedCulture = c))
                                .ToList()));
                    });
                }
                else
                {
                    // Placeholder: keeps columns 1-2 occupied so Xenotype stays at columns 3-4.
                    grid.GridItem(colSpan: 2);
                }

                if (ModsConfig.BiotechActive)
                {
                    grid.GridItem(draw: r => Verse.Widgets.Label(r, "Xenotype"));
                    grid.GridItem(draw: r =>
                    {
                        if (Verse.Widgets.ButtonText(r, _selectedXenotype?.LabelCap ?? "None"))
                            Find.WindowStack.Add(new FloatMenu(xenotypes
                                .Select(x => new FloatMenuOption(x.LabelCap, () => _selectedXenotype = x))
                                .Append(new FloatMenuOption("None", () => _selectedXenotype = null))
                                .ToList()));
                    });
                }
                else
                {
                    // Placeholder: keeps columns 3-4 occupied.
                    grid.GridItem(colSpan: 2);
                }

                // ── Row 2: Gender + Keep last name ────────────────────────────
                grid.GridItem(draw: r => Verse.Widgets.Label(r, "Gender"));
                grid.GridItem(draw: r =>
                {
                    if (Verse.Widgets.ButtonText(r, _selectedGender.GetLabel().CapitalizeFirst()))
                        Find.WindowStack.Add(new FloatMenu(
                            new List<Gender> { Gender.Male, Gender.Female }
                                .Select(g => new FloatMenuOption(g.GetLabel().CapitalizeFirst(), () => _selectedGender = g))
                                .ToList()));
                });
                grid.GridItem(colSpan: 2,
                    draw: r => Verse.Widgets.CheckboxLabeled(r, "Keep last name", ref _keepLastName));

                // ── Row 3: Force no nickname ───────────────────────────────────
                grid.GridItem(colSpan: 2,
                    draw: r => Verse.Widgets.CheckboxLabeled(r, "Force no nickname", ref _forceNoNick));

                // ── Row 4: Current name preview + Generate ─────────────────────
                grid.GridItem(colStart: 1, colSpan: 4, draw: r =>
                {
                    using (new TextBlock(TextAnchor.MiddleLeft, ColoredText.SubtleGrayColor))
                        Verse.Widgets.Label(r.TakeLeftPart((float)(r.width * 0.4)), p.Name.ToStringFull);

                    if (Verse.Widgets.ButtonText(r, "Generate"))
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        string? lastName = null;
                        if (_keepLastName && p.Name is NameTriple triple) lastName = triple.Last;
                        p.Name = PawnBioAndNameGenerator.GenerateFullPawnName(p.def,
                            p.kindDef.GetNameMaker(p.gender), p.story,
                            _selectedXenotype, p.RaceProps.GetNameGenerator(_selectedGender),
                            _selectedCulture, p.IsCreepJoiner, _selectedGender,
                            p.RaceProps.nameCategory, lastName, _forceNoNick);
                    }
                });
            });

        // Auto-resize: Rows rows × ButtonHeight + (Rows-1) gaps.
        var expectedHeight = Rows * UIUtility.ButtonHeight + (Rows - 1) * Gap;
        if (!Mathf.Approximately(windowRect.height, expectedHeight))
            windowRect.height = expectedHeight + Margin * 2;
    }
}
