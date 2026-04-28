using RimWorld;
using UnityEngine;
using Verse;
using Void.XMLComponents;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_Faction(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p) => base.ShowSection(p) && p.def.CanHaveFaction;


    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        var (label, icon, color) = FactionUtility.GetFactionMeta(pawn.Faction);

        layout.ComponentById<TextElement>("text").Content = "Faction";
        layout.ComponentById<ButtonElement>("button").Label = label;
        layout.ComponentById<ButtonElement>("button").Icon = icon;
        layout.ComponentById<ButtonElement>("button").IconColor = color;
        layout.ComponentById<ButtonElement>("button").OnClick = _ =>
        {
            List<FloatMenuOption> opts =
            [
                ..Find.FactionManager.AllFactionsInViewOrder.Prepend(null).Select(f =>
                {
                    var (l, i, c) = FactionUtility.GetFactionMeta(f);
                    return new FloatMenuOption(l, () => { FactionUtility.SetFaction(pawn, f); }, i, c,
                        f == null ? MenuOptionPriority.Low :
                        f.IsPlayer ? MenuOptionPriority.High : MenuOptionPriority.Default);
                }),
                new("Randomize", () =>
                {
                    Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out var f, false);
                    // TODO(FIXME): Currently raises error when humanlike pawn is added to mechanoid faction.
                    if (f != null) FactionUtility.SetFaction(pawn, f);
                    else Messages.Message("No valid faction found", MessageTypeDefOf.RejectInput);
                }, TexPawnEditor.Randomize, Color.white, MenuOptionPriority.VeryLow)
            ];
            Find.WindowStack.Add(new FloatMenu(opts));
        };
        layout.ComponentById<ButtonElement>("button").OnHover = r =>
        {
            TooltipHandler.TipRegion(r, FactionUtility.GetFactionTooltip(pawn.Faction));
        };
    }
}