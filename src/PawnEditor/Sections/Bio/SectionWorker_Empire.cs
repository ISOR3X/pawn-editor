using RimWorld;
using Verse;
using Void.Components;
using Void.XMLComponents;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_Empire(SectionDef def) : SectionWorker(def)
{
    private float val = 10;
    
    public override void OnLayout(Layout layout, Pawn pawn)
    {
        var curTitle = pawn.royalty.GetCurrentTitle(Faction.OfEmpire);
        layout.ComponentById<ButtonElement>("role").Label = curTitle?.GetLabelCapFor(pawn) ?? "None".Translate();
        layout.ComponentById<DivElement>("honor").Children = b =>
        {
            b.InputRange(ref val);
        };
    }
}