using HotSwap;
using RimWorld;
using Taffy;
using Verse;

namespace PawnEditor.Table;

[HotSwappable]
public class RowFilter_BackstoryDefSpawnCategory : IRowFilter<BackstoryDef>
{
    private string? _selected;

    public bool Passes(BackstoryDef row, ITableContext? ctx) =>
        _selected == null || row.spawnCategories.Contains(_selected);

    private static List<string> _spawnCategories =>
        DefDatabase<BackstoryDef>.AllDefs.SelectMany(bd => bd.spawnCategories).Distinct().ToList();

    public void DrawFilter(TaffyBuilder builder, Table<BackstoryDef> table)
    {
        builder.Text("Spawn category", font: GameFont.Tiny);

        foreach (var b in _spawnCategories)
        {
            builder.Div(build =>
            {
                    
            });
        }


        builder.Button($"Category: {_selected ?? "All"}",
            style: new Style { size = new Size<Dimension>(Dimension.Percent(1f), Dimension.AUTO) }, onClick: (_) =>
            {
                var opts = DefDatabase<BackstoryDef>.AllDefs.SelectMany(bd => bd.spawnCategories).Distinct()
                    .Select(b => new FloatMenuOption(b.ReadableCamelCase(), () =>
                    {
                        _selected = b;
                        table.SetDirty();
                    })).Prepend(new FloatMenuOption("All", () =>
                    {
                        _selected = null;
                        table.SetDirty();
                    })).ToList();

                Find.WindowStack.Add(new FloatMenu(opts));
            });
    }
}