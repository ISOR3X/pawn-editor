using Verse;
using Void.XMLComponents;

namespace Void;

/// <summary>
///     Frame-scoped mutable wrapper around an immutable <see cref="ParsedLayout" /> template tree.
///     A fresh instance is created each frame; C# registers per-element overrides via
///     <see cref="ComponentById{T}" />, then <see cref="Render" /> walks the tree and emits
///     everything into the <see cref="TaffyBuilder" />. Discarded after the frame.
/// </summary>
public class Layout(ParsedLayout template, IContext? context = null)
{
    private readonly Dictionary<string, XMLComponent> _overrides = [];

    /// <summary>
    ///     Returns a cloned, mutable copy of the element with the given <paramref name="id" />.
    ///     The returned instance is registered as this frame's override - mutate it freely.
    /// </summary>
    public T ComponentById<T>(string id) where T : XMLComponent
    {
        if (!_overrides.TryGetValue(id, out var config))
        {
            var node = template.FindById(id)
                       ?? throw new Exception($"[{VoidMod.ModName}] No element with id '{id}' in layout");
            config = node.Props.Clone();
            _overrides[id] = config;
        }

        return config as T
               ?? throw new Exception($"[{VoidMod.ModName}] Element '{id}' is not a {typeof(T).Name}");
    }

    /// <summary>
    ///     Renders the layout into <paramref name="builder" />.
    ///     When <paramref name="parentSuppliedStyle" /> is supplied and the layout has exactly one root node,
    ///     the tab style is merged onto that root (tab wins on conflict) so no extra wrapper div is
    ///     needed. When there are multiple root nodes the tab style cannot be applied; a dev-mode
    ///     warning is logged.
    /// </summary>
    public void Render(TaffyBuilder builder, StyleOverride? parentSuppliedStyle = null)
    {
        var roots = template.Children;

        if (parentSuppliedStyle != null)
        {
            if (roots.Count == 1)
            {
                var rootNode = roots[0];
                var config = GetConfigForNode(rootNode).Clone();
                config.Style = parentSuppliedStyle.Merge(config.Style); // tab wins
                Action<TaffyBuilder>? xmlChildren = rootNode.Children.Count > 0
                    ? inner =>
                    {
                        foreach (var child in rootNode.Children) RenderNode(inner, child);
                    }
                    : null;
                config.SetContext(context);
                config.Render(builder, xmlChildren);
            }
            else
            {
                if (Prefs.DevMode)
                    Log.Warning(
                        $"[{VoidMod.ModName}] Section has multiple root nodes: " +
                        "tab styles cannot be applied. Wrap contents in a single root <div>.");
                foreach (var child in roots) RenderNode(builder, child);
            }

            return;
        }

        // No tab style - render children directly into builder (no _template wrapper div).
        foreach (var child in roots) RenderNode(builder, child);
    }

    /// <summary>Returns the effective config for a node - the C# override if one was registered, otherwise the XML props.</summary>
    private XMLComponent GetConfigForNode(ParsedLayout node)
    {
        return node.Id != null && _overrides.TryGetValue(node.Id, out var ov) ? ov : node.Props;
    }

    private void RenderNode(TaffyBuilder builder, ParsedLayout node)
    {
        var config = node.Id != null && _overrides.TryGetValue(node.Id, out var ov)
            ? ov
            : node.Props;

        config.SetContext(context);

        // For container nodes whose Children wasn't overridden in C#, fall back to rendering
        // XML children recursively.
        Action<TaffyBuilder>? xmlChildren = node.Children.Count > 0
            ? inner =>
            {
                foreach (var child in node.Children) RenderNode(inner, child);
            }
            : null;

        config.Render(builder, xmlChildren);
    }
}