using System.Linq;
using RimWorld;
using Verse;

namespace PawnEditor.Table;

/// <summary>
/// Filters out backstories whose disallowed traits conflict with traits the pawn already has.
/// Requires a <see cref="PawnContext"/>; passes all rows when no context is present.
/// </summary>
public sealed class PawnCompatibleFilter : IRowFilter<BackstoryDef>
{
    public bool Enabled = true;

    public bool Passes(BackstoryDef row, ITableContext? ctx)
    {
        if (!Enabled || ctx is not ITableContext<Pawn> pawnCtx)
            return true;

        var pawn = pawnCtx.Value;
        return row.disallowedTraits.NullOrEmpty()
               || !pawn.story.traits.allTraits.Any(t =>
                   Enumerable.Any(row.disallowedTraits, dt => dt.def == t.def));
    }
}
