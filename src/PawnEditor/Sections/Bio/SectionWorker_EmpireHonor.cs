using System.Globalization;
using RimWorld;
using Taffy;
using UnityEngine;
using Verse;
using Void;
using Void.Components;
using Void.XMLComponents;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_EmpireHonor(SectionDef def) : SectionWorker_EmpireTitle(def)
{
    public override bool ShowSection(Pawn p)
    {
        if (!base.ShowSection(p)) return false;
        var curTitle = p.royalty.GetCurrentTitle(Faction.OfEmpire);
        return curTitle?.GetNextTitle(Faction.OfEmpire) != null;
    }

    public override void OnLayout(Layout layout, Pawn pawn)
    {
        var empire = Faction.OfEmpire;
        var curTitle = pawn.royalty.GetCurrentTitle(empire);
        var minMaxHonor = new IntRange(0, curTitle.GetNextTitle(empire).favorCost);
        float favor = pawn.royalty.GetFavor(empire);
        layout.ComponentById<TextElement>("min").Content = minMaxHonor.min.ToString();
        layout.ComponentById<TextElement>("max").Content = minMaxHonor.max.ToString();
        layout.ComponentById<TextElement>("value").Content = favor.ToString(CultureInfo.InvariantCulture);
        layout.ComponentById<DivElement>("honor").Children = b =>
        {
            b.InputRange(ref favor, minMaxHonor.min, minMaxHonor.max,
                style: new StyleOverride { width = Dimension.Percent(1f) });
            if (!Mathf.Approximately(favor, pawn.royalty.GetFavor(empire)))
                pawn.royalty.SetFavor(empire, (int)favor, false);
        };
    }
}