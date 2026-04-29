using Taffy;
using Verse;
using Void;
using Void.Components;

namespace PawnEditor.Table;

public sealed class RowFilter_DefContentSource<TDef> : RowFilter<TDef> where TDef : Def
{
    private ModContentPack? _selected;

    public override bool Passes(TDef row)
    {
        return _selected == null || row.modContentPack == _selected;
    }

    public override void DrawFilter(TaffyBuilder builder)
    {
        builder.Text("Content source", style: new StyleOverride { fontSize = GameFont.Tiny });
        builder.Button(_selected?.Name ?? "Any",
            style: new StyleOverride
            {
                width = Dimension.Percent(1f),
                margin = new Rect<LengthPercentageAuto>(0f, 0f, 0f, GenUI.GapSmall)
            }, onClick: _ =>
            {
                var opts = LoadedModManager.RunningMods
                    .Where(pack => pack.AllDefs.OfType<TDef>().Any())
                    .Select(pack => new FloatMenuOption(pack.Name, () =>
                    {
                        _selected = pack;
                        MarkDirty();
                    }))
                    .Prepend(new FloatMenuOption("Any", () =>
                    {
                        _selected = null;
                        MarkDirty();
                    }))
                    .ToList();
                Find.WindowStack.Add(new FloatMenu(opts));
            });
    }
}