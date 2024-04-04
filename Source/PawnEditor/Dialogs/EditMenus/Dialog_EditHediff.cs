using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
public class Dialog_EditHediff : Dialog_EditItem<Hediff>
{
    public Dialog_EditHediff(Hediff item, Pawn pawn = null, UITable<Pawn> table = null) : base(item, pawn, table) { }

    protected override void DoContents(Listing_Standard listing)
    {
        if (Selected is Hediff_Level hediffLevel)
        {
            var level = listing.SliderLabeled("Level".Translate(), hediffLevel.level, 0, Selected.def.maxSeverity, LABEL_WIDTH_PCT);
            hediffLevel.SetLevelTo(Mathf.RoundToInt(level));
        }
        else if (Selected.def.stages?.Count > 1)
            Selected.Severity = listing.SliderLabeled("PawnEditor.Severity".Translate(), Selected.Severity, 0, Selected.def.maxSeverity, LABEL_WIDTH_PCT,
                Selected.SeverityLabel);

        if (listing.ButtonTextLabeledPct("PawnEditor.BodyPart".Translate(), Selected.Part?.LabelCap ?? "WholeBody".Translate(), LABEL_WIDTH_PCT,
                TextAnchor.MiddleLeft))
        {
            IEnumerable<BodyPartRecord> parts = Pawn.RaceProps.body.AllParts;
            var options = new List<FloatMenuOption>();

            if (PawnEditorMod.Settings.HediffLocationLimit == PawnEditorSettings.HediffLocation.All
             || !ListingMenu_Hediffs.defaultBodyParts.ContainsKey(Selected.def)) options.Add(new("WholeBody".Translate(), () => Selected.Part = null));

            if (PawnEditorMod.Settings.HediffLocationLimit == PawnEditorSettings.HediffLocation.RecipeDef
             && ListingMenu_Hediffs.defaultBodyParts.TryGetValue(Selected.def, out var result))
                parts = parts.Where(part => result.Item1.Contains(part.def) || result.Item2.Any(group => part.groups.Contains(group)));

            options.AddRange(parts.Select(part => new FloatMenuOption(part.LabelCap, () => Selected.Part = part)));

            Find.WindowStack.Add(new FloatMenu(options));
        }

        if (Selected is HediffWithComps hediff)
            foreach (var comp in hediff.comps)
                switch (comp)
                {
                    case HediffComp_Immunizable immunizable:
                        Pawn.health.immunity.TryAddImmunityRecord(Selected.def, Selected.def);
                        var immunity = Pawn.health.immunity.GetImmunityRecord(Selected.def);
                        immunity.immunity = listing.SliderLabeled("Immunity".Translate(), immunity.immunity, 0, 1, LABEL_WIDTH_PCT,
                            immunizable.CompTipStringExtra);
                        break;
                    case HediffComp_Disappears disappears:
                        var progress = 1 - disappears.Progress;
                        progress = Widgets.HorizontalSlider(listing.GetRectLabeled("TimeLeft".Translate().CapitalizeFirst(), CELL_HEIGHT), progress,
                            0, 1, true, disappears.ticksToDisappear.ToStringTicksToPeriodVerbose(), "0 " + "SecondsLower".Translate(),
                            disappears.disappearsAfterTicks.ToStringTicksToPeriodVerbose());
                        disappears.ticksToDisappear = Mathf.RoundToInt(progress * Math.Max(1, disappears.disappearsAfterTicks));
                        break;
                    case HediffComp_GetsPermanent permanent:
                        var isPermanent = permanent.IsPermanent;
                        listing.CheckboxLabeled("PawnEditor.IsPermanent".Translate(), ref isPermanent, null, CELL_HEIGHT, LABEL_WIDTH_PCT);
                        permanent.IsPermanent = isPermanent;
                        break;
                }
    }
}
