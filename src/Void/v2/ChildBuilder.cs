using Taffy;
using UnityEngine;
using Verse;

namespace Void.v2;

/// <summary>
///     Scoped view over a <see cref="VoidTree" /> for describing one node's children. Obtained via
///     <see cref="VoidTree.Build" /> (for the root) or <see cref="Div" /> (for a nested container) —
///     never constructed directly, so there's no way to describe children without the scope that
///     commits them when it ends.
/// </summary>
public sealed class ChildBuilder
{
    private readonly VoidTree _tree;
    private readonly TaffyNode _parent;

    internal ChildBuilder(VoidTree tree, TaffyNode parent)
    {
        _tree = tree;
        _parent = parent;
    }

    /// <summary>Adds or reuses a leaf/plain child identified by <paramref name="key" />.</summary>
    public TaffyNode Item(string key, VStyle style, TreeContext? context = null, bool isLeaf = false,
        Action<Rect>? draw = null) =>
        _tree.Upsert(_parent, key, style, context, isLeaf, draw);

    /// <summary>
    ///     Adds or reuses a container child identified by <paramref name="key" />, then runs
    ///     <paramref name="children" /> against a builder scoped to it — committed automatically
    ///     when <paramref name="children" /> returns, including on exception.
    /// </summary>
    public TaffyNode Div(string key, VStyle style, Action<ChildBuilder> children, Action<Rect>? draw = null)
    {
        var node = _tree.Upsert(_parent, key, style, draw: draw);
        var childBuilder = new ChildBuilder(_tree, node);
        try
        {
            children(childBuilder);
        }
        finally
        {
            _tree.FinishChildren(node);
        }

        return node;
    }

    /// <summary>Adds or reuses a text leaf, handling the usual word-wrap/label boilerplate.</summary>
    public TaffyNode Text(string key, string text, GameFont font = GameFont.Small,
        TextAnchor align = TextAnchor.UpperLeft, VStyle? style = null) =>
        Item(key,
            style ?? new VStyle(Width: Dimension.Percent(1f)),
            new TreeContext(Text: text, Font: font),
            isLeaf: true,
            draw: r =>
            {
                using (new TextBlock(font, align))
                {
                    Verse.Text.WordWrap = true;
                    Verse.Widgets.Label(r, text);
                }
            });
}
