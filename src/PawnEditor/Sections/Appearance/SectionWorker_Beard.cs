using System;
using System.Collections.Generic;
using PawnEditor.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[Reloadable]
public class SectionWorker_Beard : SectionWorker
{
    private const int RowCount = 11;
    private const float RowHeight = 30f;
    private readonly TableWorker<Def> _beardDefTable;

    public SectionWorker_Beard(SectionDef def) : base(def)
    {
        var beards = DefDatabase<BeardDef>.AllDefs;
        _beardDefTable = (DefTableWorker_Hair)Activator.CreateInstance(TableDefOf.PawnEditor_Beards.workerClass,
            TableDefOf.PawnEditor_Beards, (Func<IEnumerable<Def>>)(() => beards), BeardDefOf.NoBeard);
    }

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        listing.LabelH2("Beard");

        var beardTableRect =
            listing.GetRect(_beardDefTable.HeaderHeight + (RowCount + 1) * RowHeight + UIUtility.ButtonHeight + 4f);

        if (pawn.style.CanWantBeard || PawnEditorMod.Settings.Restriction == Settings.RestrictionMode.None)
            _beardDefTable.TableOnGUI(beardTableRect);
        else
            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                Verse.Widgets.Label(beardTableRect,
                    $"No beards available for {pawn.Name.ToStringShort}".Colorize(ColoredText.SubtleGrayColor));
            }
    }
}