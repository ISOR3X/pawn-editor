using Taffy;
using UnityEngine;
using Verse;

namespace PawnEditor;

/// <summary>
/// Base class for all XML-driven UI elements. XML props are set at parse time; C# overrides
/// any field per-frame via <see cref="UILayout.ComponentById{T}"/>. Each subclass owns its
/// rendering logic via <see cref="Render"/>.
/// </summary>
public abstract class UIElement
{
    public StyleOverride Style = new();

    /// <summary>Raw untyped attributes from XML, for extensibility by other mods.</summary>
    public readonly Dictionary<string, string> Attrs = [];

    public string? Get(string key) => Attrs.GetValueOrDefault(key);

    public T? Get<T>(string key, Func<string, T> parse) =>
        Attrs.TryGetValue(key, out var v) ? parse(v) : default;

    public abstract UIElement Clone();

    /// <summary>
    /// Emits this element into the builder. <paramref name="children"/> is a callback that renders
    /// XML children recursively for container types; it is null for leaf types.
    /// </summary>
    public abstract void Render(TaffyBuilder builder, Action<TaffyBuilder>? children);
}

public class ButtonElement : UIElement
{
    public string? Label;
    public Texture2D? Icon;
    /// <summary>Icon name from XML, resolved lazily on first render to avoid loading textures at parse time.</summary>
    public string? IconName;
    public Action<Rect>? OnClick;
    public Action<Rect>? OnHover;

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
    {
        // Resolve icon name lazily — UIIcons accesses textures which aren't loaded at def-load time.
        if (Icon == null && IconName != null)
            Icon = UIIcons.Resolve(IconName);
        builder.Button(Label, Icon, onClick: OnClick, onHover: OnHover, style: Style);
    }

    public override UIElement Clone() => (ButtonElement)MemberwiseClone();
}

public class DivElement : UIElement
{
    /// <summary>
    /// If set, C# fully owns children. If null, XML children are rendered via the default tree walk.
    /// </summary>
    public Action<TaffyBuilder>? Children;

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
        => builder.Div(Children ?? children ?? (_ => { }), Style);

    public override UIElement Clone() => (DivElement)MemberwiseClone();
}

public class TextElement : UIElement
{
    public string? Content;
    public Color? Color;

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
        => builder.Text(Content ?? string.Empty, color: Color, style: Style);

    public override UIElement Clone() => (TextElement)MemberwiseClone();
}

/// <summary>
/// Represents a <c>&lt;section&gt;</c> element within a layout tree. Delegates rendering to the
/// referenced <see cref="SectionDef"/>'s worker, applying tab-level style composition.
/// The <see cref="Pawn"/> property is set by <see cref="UILayout.RenderNode"/> before
/// <see cref="Render"/> is called.
/// </summary>
public class SectionElement : UIElement
{
    public SectionDef? ResolvedDef;

    /// <summary>Set by UILayout.RenderNode before each call to Render.</summary>
    internal Pawn? Pawn;

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
    {
        var worker = ResolvedDef?.Worker;
        if (worker == null || Pawn == null) return;
        // Delegate entirely to BuildSection — it handles both UILayout (merge) and legacy (wrap).
        worker.BuildSection(builder, Pawn, Style);
    }

    public override UIElement Clone() => (SectionElement)MemberwiseClone();
}
