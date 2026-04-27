using System.Globalization;
using RimWorld;
using Verse;
using Void;
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
        
        var slider = layout.ComponentById<InputElement>("honor");
        slider.Value = new Reactive<int>(() => pawn.royalty.GetFavor(empire),
            v => pawn.royalty.SetFavor(empire, v, false));
        slider.MinMax = new FloatRange(minMaxHonor.min, minMaxHonor.max);
    }
}