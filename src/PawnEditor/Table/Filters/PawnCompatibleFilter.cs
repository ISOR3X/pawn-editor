using PawnEditor.TaffySharp;
using RimWorld;
using Verse;

namespace PawnEditor.Table;

/// <summary>
/// Filters out backstories whose disallowed traits conflict with traits the pawn already has.
/// Requires a <see cref="PawnContext"/>; passes all rows when no context is present.
/// </summary>
public sealed class PawnCompatibleFilter : IRowFilter<BackstoryDef>
{
    private bool _enabled = true;

    public bool Passes(BackstoryDef row, ITableContext? ctx)
    {
        if (!_enabled || ctx is not ITableContext<Pawn> pawnCtx)
            return true;

        var pawn = pawnCtx.Value;
        return row.disallowedTraits.NullOrEmpty()
               || !pawn.story.traits.allTraits.Any(t =>
                   Enumerable.Any(row.disallowedTraits, dt => dt.def == t.def));
    }

    public void DrawFilter(TaffyBuilder builder, Table<BackstoryDef> table)
    {
        builder.Item(new Style(), draw: r =>
        {
            var wasEnabled = _enabled;
            Verse.Widgets.CheckboxLabeled(r, "Compatible with pawn", ref _enabled);
            if (_enabled != wasEnabled)
                table.SetDirty();
        });
    }
}