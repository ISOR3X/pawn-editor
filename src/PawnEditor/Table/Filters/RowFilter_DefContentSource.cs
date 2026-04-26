using RimWorld;
using Taffy;
using Verse;
using Void;
using Void.Components;

namespace PawnEditor.Table;

public sealed class RowFilter_DefContentSource<TDef> : IRowFilter<TDef> where TDef : Def
{
    private ModContentPack? _selected;

    public bool Passes(TDef row, IContext? ctx)
    {
        return _selected == null || row.modContentPack == _selected;
    }

    public void DrawFilter(TaffyBuilder builder, Table<TDef> table)
    {
        builder.Text("Content source", style: new  StyleOverride { fontSize = GameFont.Tiny });
        builder.Button(_selected?.Name ?? "Any",
            style: new StyleOverride
            {
                width = Dimension.Percent(1f),
                margin = new Rect<LengthPercentageAuto>(0f, 0f, 0f, GenUI.GapSmall)
            }, onClick: _ =>
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