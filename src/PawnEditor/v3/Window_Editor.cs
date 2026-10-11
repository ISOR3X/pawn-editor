using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Void.Native;

namespace Editor;

/// <summary>
///     Test window for the stack using live game data: World -> factions -> pawns -> gear/things.
///     Every entity links back to what holds or owns it, so cycles are easy to produce.
/// </summary>
public class Window_Editor : Window
{
    private const int MaxLinksPerGroup = 40; // no scrolling in this mock

    private readonly EditorState editorState = GameComponent_EditorStack.StateForCurrentGame();
    private readonly UITree tree = new();

    public override void PostClose() => tree.Dispose();

    [DebugAction("Editor", "Open editor", allowedGameStates = AllowedGameStates.Invalid)]
    public static void Open()
    {
        if (Find.WindowStack.IsOpen<Window_Editor>()) Find.WindowStack.TryRemove(typeof(Window_Editor));
        else Find.WindowStack.Add(new Window_Editor());
    }

    public override Vector2 InitialSize => Page.StandardSize;

    public Window_Editor()
    {
        // resizeable = true;
    }

    private static string LabelFor(object target) => target switch
    {
        Pawn pawn => pawn.LabelShortCap,
        Thing thing => thing.LabelCap,
        Faction faction => faction.Name,
        _ => target.ToString()
    };

    /// <summary>Groups of things the target links to. Order matters for the button ids.</summary>
    private static List<(string group, List<object> targets)> LinksOf(object? target)
    {
        var groups = new List<(string, List<object>)>();

        void Add(string name, IEnumerable<object?> items)
        {
            var list = items.OfType<object>().ToList();
            if (list.Count > 0) groups.Add((name, list));
        }

        switch (target)
        {
            case null: // root: all factions
                Add("Factions", Find.FactionManager?.AllFactionsVisible.Cast<object>() ?? []);
                break;
            case Faction faction:
                Add("Leader", [faction.leader]);
                Add("Pawns", PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead.Where(p => p.Faction == faction));
                break;
            case Pawn pawn:
                Add("Faction", [pawn.Faction]);
                Add("Equipment", pawn.equipment?.AllEquipmentListForReading ?? []);
                Add("Apparel", pawn.apparel?.WornApparel ?? []);
                Add("Inventory", pawn.inventory?.innerContainer ?? []);
                Add("Relations", pawn.relations?.DirectRelations.Select(r => r.otherPawn) ?? []);
                break;
            case Thing thing:
                Add("Held by", [HolderPawnOf(thing)]);
                break;
        }

        return groups;
    }

    /// <summary>Walks up through trackers and containers to the pawn carrying the thing, if any.</summary>
    private static Pawn? HolderPawnOf(Thing thing)
    {
        var holder = thing.ParentHolder;
        while (holder != null && holder is not Pawn) holder = holder.ParentHolder;
        return holder as Pawn;
    }

    private void AddOpenButton(UIBranch branch, object target, string id) =>
        branch.Button(LabelFor(target), onClick: _ => editorState.Push(new EditorEntry(LabelFor(target), target)),
            id: id);

    public override void DoWindowContents(Rect inRect)
    {
        tree.Build(b =>
        {
            var target = editorState.CurrentTarget;
            // Header
            b.Div(b2 =>
            {
                b2.Div(b3 =>
                {
                    var label = "World";
                    var s = new Style { width = 32f, height = 32f };

                    if (target != null)
                    {
                        Window_Editor_Utility.DrawIconFor(b3, target, style: s);
                        label = Window_Editor_Utility.LabelFor(target);
                    }
                    else
                        b3.Icon(Widgets.PlaceholderIconTex, style: s);

                    b3.Text(label, style: new Style { fontSize = GameFont.Medium });
                }, style: new Style { flexDirection = TaffyFlexDirection.Row, alignItems = TaffyAlignItems.Center, gap = new TaffyAxes(8f) });

                // Breadcrumb
                b.Div(b2 =>
                {
                    b2.Button("World", onClick: _ => editorState.PopTo(null), variant: Components.ButtonVariant.Ghost,
                        id: "crumb-world", size: Components.ComponentSize.Small);

                    for (var i = 0; i < editorState.Entries.Count; i++)
                    {
                        var entry = editorState.Entries[i];
                        b2.Icon(Void.TexUI.ArrowRight, id: $"sep-{i}", size: Components.ComponentSize.Small);
                        b2.Button(entry.label, onClick: _ => editorState.PopTo(entry),
                            variant: Components.ButtonVariant.Ghost, id: $"crumb-{i}", size: Components.ComponentSize.Small);
                    }
                }, style: new Style { flexDirection = TaffyFlexDirection.Row, alignItems = TaffyAlignItems.Center });

            }, style: new Style { flexDirection = TaffyFlexDirection.Column,gap = new TaffyAxes(8f), margin = new TaffyEdges(0f, 0f, 16f, 0f) });



            // Body: the links of the top entry, or the factions at the root
            b.Div(b2 =>
            {
                b2.Text(target == null ? "World" : $"Editing {LabelFor(target ?? "(gone)")}", id: "title");
                if (target != null) b2.Button("Back", onClick: _ => editorState.Pop(), id: "back");

                foreach (var (group, targets) in LinksOf(target))
                {
                    b2.Text($"{group} ({targets.Count})", id: $"group-{group}");
                    for (var i = 0; i < Math.Min(targets.Count, MaxLinksPerGroup); i++)
                        AddOpenButton(b2, targets[i], $"{group}-{i}");
                }
            }, style: new Style { flexDirection = TaffyFlexDirection.Column });
        }, style: new Style { flexDirection = TaffyFlexDirection.Column });
        tree.Draw(inRect);
    }
}
