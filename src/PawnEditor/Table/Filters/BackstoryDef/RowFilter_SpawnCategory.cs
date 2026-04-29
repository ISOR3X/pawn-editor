using RimWorld;

namespace PawnEditor.Table;

public class RowFilter_SpawnCategory() : RowFilter_ToggleableList<BackstoryDef, string>("Spawn category", s => s.ReadableCamelCase(), [])
{
    protected override void Initialize(IReadOnlyList<BackstoryDef> allRows)
    {
        allOptions = [.. allRows.SelectMany(bd => bd.spawnCategories).Distinct().OrderBy(s => s)];
    }

    protected override IReadOnlyList<string>? GetOptions(BackstoryDef row) =>
        row.spawnCategories.Count > 0 ? row.spawnCategories : null;
}