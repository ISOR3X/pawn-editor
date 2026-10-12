using RimWorld;
using Verse;
using Void.Native;

namespace Editor;

/// <summary>
///     Placeholder sections that list what a target links to, as buttons that open that entity. They stand in for
///     real editing UI while the sidebar is being designed.
/// </summary>
public abstract class LinkListSection<T> : SectionWorker<T> where T : class
{
    private const int MaxItems = 40;

    protected abstract IEnumerable<object?> ItemsOf(T target);

    protected override void BuildSection(UIBranch b, T target, EditorState editor)
    {
        var items = ItemsOf(target).OfType<object>().ToList();
        if (items.Count == 0)
        {
            b.Text("None", id: "none");
            return;
        }

        for (var i = 0; i < Math.Min(items.Count, MaxItems); i++)
        {
            var item = items[i];
            var label = Window_Editor_Utility.LabelFor(item);
            b.Button(label, onClick: _ => editor.Push(new EditorEntry(label, item)), id: $"item-{i}");
        }

        if (items.Count > MaxItems) b.Text($"...and {items.Count - MaxItems} more", id: "more");
    }
}

public class Section_PawnBasics : LinkListSection<Pawn>
{
    protected override IEnumerable<object?> ItemsOf(Pawn pawn) => [pawn.Faction];
}

public class Section_PawnEquipment : LinkListSection<Pawn>
{
    protected override IEnumerable<object?> ItemsOf(Pawn pawn) => pawn.equipment?.AllEquipmentListForReading ?? [];
}

public class Section_PawnApparel : LinkListSection<Pawn>
{
    protected override IEnumerable<object?> ItemsOf(Pawn pawn) => pawn.apparel?.WornApparel ?? [];
}

public class Section_PawnInventory : LinkListSection<Pawn>
{
    protected override IEnumerable<object?> ItemsOf(Pawn pawn) => pawn.inventory?.innerContainer ?? [];
}

public class Section_PawnRelations : LinkListSection<Pawn>
{
    protected override IEnumerable<object?> ItemsOf(Pawn pawn) =>
        pawn.relations?.DirectRelations.Select(r => r.otherPawn) ?? [];
}

public class Section_FactionLeader : LinkListSection<Faction>
{
    protected override IEnumerable<object?> ItemsOf(Faction faction) => [faction.leader];
}

public class Section_FactionPawns : LinkListSection<Faction>
{
    protected override IEnumerable<object?> ItemsOf(Faction faction) =>
        PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead.Where(p => p.Faction == faction);
}

public class Section_ThingHolder : LinkListSection<Thing>
{
    protected override IEnumerable<object?> ItemsOf(Thing thing) => [HolderPawnOf(thing)];

    /// <summary>Walks up through trackers and containers to the pawn carrying the thing, if any.</summary>
    private static Pawn? HolderPawnOf(Thing thing)
    {
        var holder = thing.ParentHolder;
        while (holder != null && holder is not Pawn) holder = holder.ParentHolder;
        return holder as Pawn;
    }
}
