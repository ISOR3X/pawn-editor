using RimWorld;
using UnityEngine;
using Verse;
using Void.XMLComponents;
using Layout = Void.Layout;

namespace PawnEditor;

public class SectionWorker_Faction(SectionDef def) : SectionWorker(def)
{
    public override bool ShowSection(Pawn p)
    {
        return base.ShowSection(p) && p.def.CanHaveFaction;
    }


    protected override void OnLayout(Layout layout, Pawn pawn)
    {
        var (label, icon, color) = FactionUtility.GetFactionMeta(pawn.Faction);

        layout.ComponentById<TextElement>("text").Content = "Faction";
        layout.ComponentById<ButtonElement>("button").Label = label;
        layout.ComponentById<ButtonElement>("button").Icon = icon;
        layout.ComponentById<ButtonElement>("button").IconColor = color;
        layout.ComponentById<ButtonElement>("button").OnClick = _ =>
        {
            FloatMenuDeep? parentMenu = null;
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
                    FloatMenuDeep? level2 = null;
                    level2 = parentMenu!.OpenSubMenu(
                    [
                        // TODO: Closes too early when A1a is open and mouse moved away
                        new("A", () =>
                        {
                            FloatMenuDeep? level3 = null;
                            level3 = level2!.OpenSubMenu(
                            [
                                new("A1", () =>
                                {
                                    level3!.OpenSubMenu(
                                    [
                                        new("A1a", () => Messages.Message("A1a", MessageTypeDefOf.NeutralEvent)),
                                        new("A1b", () => Messages.Message("A1b", MessageTypeDefOf.NeutralEvent))
                                    ]);
                                }),
                                new("A2", () => Messages.Message("A2", MessageTypeDefOf.NeutralEvent))
                            ]);
                        }),
                        new("B", () => Messages.Message("B", MessageTypeDefOf.NeutralEvent))
                    ]);
                }, TexPawnEditor.Randomize, Color.white, MenuOptionPriority.VeryLow)
            ];
            parentMenu = new FloatMenuDeep(opts);
            Find.WindowStack.Add(parentMenu);
        };
        layout.ComponentById<ButtonElement>("button").OnHover = r =>
        {
            TooltipHandler.TipRegion(r, FactionUtility.GetFactionTooltip(pawn.Faction));
        };
    }
}