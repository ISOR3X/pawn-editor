using HotSwap;
using PawnEditor.Table;
using Verse;

namespace PawnEditor;

[HotSwappable]
public class FilteredDefTableWorker(DefTableDef def, Func<IEnumerable<Def>> thingsGetter, Def? defaultThing = null)
    : DefTableWorker(def, thingsGetter, defaultThing)
{
    private readonly List<TableFilter> _filters = [];

    public IReadOnlyList<TableFilter> Filters => _filters;

    public void AddFilter(TableFilter filter)
    {
        _filters.Add(filter);
        SetDirty();
    }

    public void RemoveFilter(TableFilter filter)
    {
        _filters.Remove(filter);
        SetDirty();
    }

    protected override IEnumerable<Def> SortFunction(IEnumerable<Def> input)
    {
        return _filters.Count == 0 ? input : input.Where(item => _filters.All(f => f.Matches(item)));
    }
}