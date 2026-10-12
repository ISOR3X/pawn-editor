using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Void.Native;

namespace Editor;

/// <summary>
///     Editor window: breadcrumb over a sidebar of the target's categories and a content pane of its sections.
///     Categories and sections come from defs (see <see cref="EditorRegistry" />).
/// </summary>
public class Window_Editor : Window
{
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

    private static string LabelFor(object target) => Window_Editor_Utility.LabelFor(target);

    private void DrawCategory(UIBranch b, EditorEntry entry, VisibleCategory visible, bool isSelected)
    {
        var def = visible.Def;
        var expanded = editorState.ExpandedCategories.Contains(def.defName);

        b.Div(b2 =>
        {
            b2.Button(icon: expanded ? TexButton.Collapse : TexButton.Reveal, onClick: _ =>
            {
                if (!expanded) editorState.ExpandedCategories.Add(def.defName);
                else editorState.ExpandedCategories.Remove(def.defName);
                if (!isSelected) entry.SelectCategory(def.defName);
            }, variant: Components.ButtonVariant.Ghost, style: new Style { padding = new TaffyEdges(0f) });
            b2.Button(def.Title, onClick: _ => { if (!isSelected) entry.SelectCategory(def.defName); }, variant: Components.ButtonVariant.Ghost, style: new Style { padding = new TaffyEdges(4f), width = Dimension.Percent(1f), justifyContent = TaffyAlignContent.Start });
        }, draw: r => { if (expanded) Widgets.DrawLineHorizontal(r.x, r.yMax, r.width, PawnTable.BorderColor); });

        if (!expanded) return;

        foreach (var section in visible.Sections)
        {
            var isCurrent = isSelected && entry.Section == section.defName;
            var t = section.Title;
            if (isCurrent) t = "> " + t; // Workaround until we figure out a neat way to style the background of buttons.
            b.Button(t, onClick: _ => entry.SelectSection(def.defName, section.defName),
                block: true, variant: Components.ButtonVariant.Ghost, style: new Style { padding = new TaffyEdges(4f, 0f, 0f, 32f), width = Dimension.Percent(1f), justifyContent = TaffyAlignContent.Start });
        }

        // b.Collapsible(editorState.ExpandedCategories.Contains(def.defName), open =>
        // {
        //     if (open) editorState.ExpandedCategories.Add(def.defName);
        //     else editorState.ExpandedCategories.Remove(def.defName);
        //     if (!isSelected) entry.SelectCategory(def.defName);
        // }, def.Title, content =>
        // {
        //     foreach (var section in visible.Sections)
        //     {
        //         var isCurrent = isSelected && entry.Section == section.defName;
        //         content.Button(section.Title, onClick: _ => entry.SelectSection(def.defName, section.defName),
        //             block: true, size: Components.ComponentSize.Small,
        //             variant: isCurrent ? Components.ButtonVariant.Solid : Components.ButtonVariant.Ghost,
        //             id: $"sec-{section.defName}");
        //     }
        // }, id: $"cat-{def.defName}");
    }

    private void Select(EditorEntry entry, CategoryDef def)
    {
        entry.SelectCategory(def.defName);
        editorState.ExpandedCategories.Add(def.defName);
    }

    /// <summary>The root view stands in for the browser: all visible factions.</summary>
    private void DrawRoot(UIBranch b)
    {
        var factions = Find.FactionManager?.AllFactionsVisible.ToList() ?? [];
        b.Text($"Factions ({factions.Count})", id: "factions");
        for (var i = 0; i < factions.Count; i++)
        {
            var faction = factions[i];
            b.Button(LabelFor(faction), onClick: _ => editorState.Push(new EditorEntry(LabelFor(faction), faction)),
                id: $"faction-{i}");
        }
    }

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

            }, style: new Style { flexDirection = TaffyFlexDirection.Column, gap = new TaffyAxes(8f), margin = new TaffyEdges(0f, 0f, 16f, 0f) });

            var entry = editorState.CurrentEntry;
            var visible = target != null ? EditorRegistry.VisibleFor(target) : [];
            var selected = visible.FirstOrDefault(v => v.Def.defName == entry?.Category) ?? visible.FirstOrDefault();

            b.Div(b2 =>
            {
                // Sidebar: categories of the target's type, with their sections when expanded.
                // Hidden when the target has none, so the content pane gets the full width.
                if (visible.Count > 0)
                    b2.Div(b3 =>
                    {
                        foreach (var category in visible)
                            DrawCategory(b3, entry!, category, category == selected);
                    },
                    style: new Style
                    {
                        width = 196f,
                        height = Dimension.Percent(1f),
                        flexShrink = 0f,
                        flexDirection = TaffyFlexDirection.Column,
                        overflowY = TaffyOverflow.Scroll,
                        padding = new TaffyEdges(4f)
                    });

                // Content: every section of the selected category, stacked
                b2.Div(b3 =>
                {
                    if (target == null) DrawRoot(b3);
                    else if (selected == null) b3.Text("Nothing to edit for this type.", id: "empty");
                    else
                        foreach (var section in selected.Sections)
                        {
                            b3.SectionHeading(section.Title);
                            b3.Div(b4 =>
                            {
                                section.Worker.Build(b4, target, editorState);
                            }, id: $"section-{section.defName}",
                            style: new Style
                            {
                                flexDirection = TaffyFlexDirection.Column,
                                gap = new TaffyAxes(4f),
                                margin = new TaffyEdges(4f, 0f, 16f, 8f)
                            });
                        }
                }, style: new Style
                {
                    flexDirection = TaffyFlexDirection.Column,
                    flexGrow = 1f,
                    overflowY = TaffyOverflow.Scroll
                });

            }, style: new Style { flexDirection = TaffyFlexDirection.Row, height = Dimension.Percent(1f), gap = new TaffyAxes(16f) });

        }, style: new Style { flexDirection = TaffyFlexDirection.Column, width = Dimension.Percent(1f), height = Dimension.Percent(1f) });
        tree.Draw(inRect);
    }
}
