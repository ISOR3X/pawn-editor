using HotSwap;
using Verse;

namespace PawnEditor.Table;

[HotSwappable]
public class DefTableFilter_ContentSource : DefTableFilter_FloatMenu<ModContentPack>
{
    protected override string Label => "Content source";

    protected override IEnumerable<ModContentPack> Options =>
        LoadedModManager.runningMods.Where(m => m.AllDefs.OfType<ThingDef>().Any());

    protected override string GetLabel(ModContentPack option) => option.Name;

    protected override bool MatchesOption(Def thing, ModContentPack option) =>
        thing.modContentPack == option;
}