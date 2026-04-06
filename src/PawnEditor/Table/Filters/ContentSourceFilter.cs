using Taffy;
using RimWorld;
using Verse;

namespace PawnEditor.Table;

public sealed class ContentSourceFilter<TDef> : IRowFilter<TDef> where TDef : Def
{
    private ModContentPack? _selected;

    public bool Passes(TDef row, ITableContext? ctx)
        => _selected == null || row.modContentPack == _selected;

    public void DrawFilter(TaffyBuilder builder, Table<TDef> table)
    {
        builder.Button($"Source: {_selected?.Name ?? "Any"}",
            style: new Style { size = new Size<Dimension>(Dimension.Percent(1f), Dimension.AUTO) }, onClick: (_) =>
            {
                var opts = LoadedModManager.RunningMods
                    .Where(pack => pack.AllDefs.OfType<BackstoryDef>().Any())
                    .Select(pack => new FloatMenuOption(pack.Name, () =>
                    {
                        _selected = pack;
                        table.SetDirty();
                    }))
                    .Prepend(new FloatMenuOption("Any", () =>
                    {
                        _selected = null;
                        table.SetDirty();
                    }))
                    .ToList();
                Find.WindowStack.Add(new FloatMenu(opts));
            });
    }
}