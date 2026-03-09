using System;
using System.Linq;
using HotSwap;
using RimWorld;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class SectionWorker_BioFaction : SectionWorker
{
    public SectionWorker_BioFaction(SectionDef def) : base(def)
    {
    }

    public static TipSignal FactionTooltip(Faction faction)
    {
        // REF: FactionUIUtility.DrawFactionRow
        return new TipSignal(
            (Func<string>)(() =>
                $"{faction.Name.Colorize(ColoredText.TipSectionTitleColor)}\n{faction.def.LabelCap.Resolve()}\n\n{faction.def.Description}"),
            faction.loadID ^ 1938473043);
    }

    private static void SetFaction(Pawn pawn, Faction faction)
    {
        pawn.SetFaction(faction);
        var editorWindow = Find.WindowStack.Windows.OfType<Window_Editor>().FirstOrDefault();
        editorWindow?.TrySelect(pawn);
        editorWindow?.TrySelect(faction);
    }

    protected override void DoSectionContents(Listing_Standard listing, Pawn pawn)
    {
        if (!pawn.def.CanHaveFaction) return;

        var r = listing.GetRect(UIUtility.ButtonHeight);

        if (UIUtility.ButtonTextLabeled_WithIcon(r, "Faction", pawn.Faction.Name, pawn.Faction.def.FactionIcon,
                pawn.Faction.Color))
            Find.WindowStack.Add(new FloatMenu(Find.FactionManager.AllFactionsInViewOrder.Select(f =>
            {
                return new FloatMenuOption(f.Name, () => { SetFaction(pawn, f); }, f.def.FactionIcon, f.Color);
            }).ToList()));

        if (Mouse.IsOver(r))
        {
            TooltipHandler.TipRegion(r, FactionTooltip(pawn.Faction));
        }
    }
}