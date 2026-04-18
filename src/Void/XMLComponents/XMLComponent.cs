using System.Xml;

namespace Void.XMLComponents;

/// <summary>
///     Base class for all XML-driven UI elements. XML props are set at parse time; C# overrides
///     any field per-frame via <see cref="Layout.ComponentById{T}" />. Each subclass owns its
///     rendering logic via <see cref="Render" />.
/// </summary>
public abstract class XMLComponent
{
    /// <summary>Raw untyped attributes from XML, for extensibility by other mods.</summary>
    public readonly Dictionary<string, string> Attrs = [];

    public StyleOverride Style = new();

    /// <summary>
    ///     True if this element is a leaf node — the parser will not recurse into its XML children.
    /// </summary>
    public virtual bool IsLeaf => false;

    public string? Get(string key)
    {
        return Attrs.GetValueOrDefault(key);
    }

    public T? Get<T>(string key, Func<string, T> parse)
    {
        return Attrs.TryGetValue(key, out var v) ? parse(v) : default;
    }

    public virtual XMLComponent Clone()
    {
        return (XMLComponent)MemberwiseClone();
    }

    /// <summary>
    ///     Called by <see cref="Layout" /> before each <see cref="Render" />. Override to extract
    ///     the frame-scoped context (e.g. <c>(context as IContext&lt;Pawn&gt;)?.Value</c>).
    ///     No-op by default.
    /// </summary>
    public virtual void SetContext(IContext? context)
    {
    }

    /// <summary>
    ///     Called by <see cref="XMLLayoutParser" /> after element creation.
    ///     Override to read element-specific XML attributes into typed fields.
    ///     Attributes not consumed here are collected into <see cref="Attrs" /> by the parser.
    /// </summary>
    public virtual void ParseXmlAttrs(XmlNode node)
    {
    }

    /// <summary>
    ///     Emits this element into the builder. <paramref name="children" /> is a callback that renders
    ///     XML children recursively for container types; it is null for leaf types.
    /// </summary>
    public abstract void Render(TaffyBuilder builder, Action<TaffyBuilder>? children);
}