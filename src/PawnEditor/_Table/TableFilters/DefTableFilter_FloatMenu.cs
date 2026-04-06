using HotSwap;
using Verse;

namespace PawnEditor.Table;

[HotSwappable]
public abstract class DefTableFilter_FloatMenu<T> : TableFilter where T : class
{
    private T? _selected;

    protected abstract IEnumerable<T> Options { get; }

    protected abstract string GetLabel(T option);

    protected abstract bool MatchesOption(Def thing, T option);

    protected override void DrawFilterWidget(Listing_Standard listing, Action? onChanged = null)
    {
        var buttonLabel = _selected != null ? GetLabel(_selected) : "Any";
        if (listing.ButtonText(buttonLabel))
        {
            var opts = Options.Select(o => new FloatMenuOption(GetLabel(o), () =>
            {
                _selected = o;
                onChanged?.Invoke();
            })).ToList();
            opts.Add(new FloatMenuOption("Any", () =>
            {
                _selected = null;
                onChanged?.Invoke();
            }));
            Find.WindowStack.Add(new FloatMenu(opts));
        }
    }

    protected override bool MatchesCore(Def thing)
    {
        if (_selected == null) return true;
        return MatchesOption(thing, _selected);
    }
}