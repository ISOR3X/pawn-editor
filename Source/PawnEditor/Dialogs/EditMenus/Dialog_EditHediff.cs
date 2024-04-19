using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimUI;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnEditor;

[UsedImplicitly]
public class Dialog_EditHediff : Dialog_EditItem<Hediff>
{
    public Dialog_EditHediff(Hediff item, Pawn pawn = null, UITable<Pawn> table = null) : base(item, pawn, table)
    {
    }

    protected override void DoContents(Listing_Horizontal listing)
    {
        if (Selected is Hediff_Level hediffLevel)
        {
            var level = listing.SliderLabeled("Level".Translate(), hediffLevel.level, 0, Selected.def.maxSeverity, 6);
            hediffLevel.SetLevelTo(Mathf.RoundToInt(level));
        }
        else if (Selected.def.stages?.Count > 1)
        {
            // Use maxSeverity or lethalSeverity if they are not default values, otherwise use 1.
            var max = Math.Abs(Selected.def.maxSeverity - float.MaxValue) != 0 ? Selected.def.maxSeverity : Math.Abs(Selected.def.lethalSeverity - -1) != 0 ? Selected.def.lethalSeverity : 1;
            var severity = Selected.Severity;
            Selected.Severity = listing.SliderLabeled("PawnEditor.Severity".Translate(), severity, 0, 1, (severity / max).ToStringPercent(), null, null, 6,
                Selected.SeverityLabel);
        }


        if (listing.ButtonTextLabeled("PawnEditor.BodyPart".Translate(), Selected.Part?.LabelCap ?? "WholeBody".Translate(), 6))
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
                        immunity.immunity = listing.SliderLabeled("Immunity".Translate(), immunity.immunity, 0, 1, (immunity.immunity / 1).ToStringPercent(), null, null, 6,
                            immunizable.CompTipStringExtra);
                        break;
                    case HediffComp_Disappears disappears:
                        var progress = 1 - disappears.Progress;
                        progress = listing.SliderLabeled("TimeLeft".Translate().CapitalizeFirst(), progress,
                            0, 1, disappears.ticksToDisappear.ToStringTicksToPeriodVerbose(), "0 " + "SecondsLower".Translate(),
                            disappears.disappearsAfterTicks.ToStringTicksToPeriodVerbose(), 6);
                        disappears.ticksToDisappear = Mathf.RoundToInt(progress * Math.Max(1, disappears.disappearsAfterTicks));
                        break;
                    case HediffComp_GetsPermanent permanent:
                        var isPermanent = permanent.IsPermanent;
                        listing.CheckboxLabeled("PawnEditor.IsPermanent".Translate(), ref isPermanent, 6);
                        permanent.IsPermanent = isPermanent;
                        break;
                }
    }
}