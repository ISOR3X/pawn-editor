using System.Xml;
using Verse;
using Void.XMLComponents;

namespace Void;

/// <summary>
/// Parses a layout XML tag (<see cref="XmlNode"/>) into a <see cref="UILayoutNode"/> tree.
/// Called from <see cref="UILayoutNode.LoadDataFromXmlCustom"/>.
/// <para>
/// Custom tags can be registered via <see cref="RegisterTag"/> before def loading begins
/// (e.g. from a <c>Mod</c> constructor). Unknown tags log a warning and are skipped.
/// </para>
/// </summary>
public static class XMLLayoutParser
{
    // Only globally-structural attributes are excluded from Attrs — they are parsed before
    // element creation and would be meaningless there.
    private static readonly HashSet<string> StructuralAttrs = ["id", "style", "class"];

    private static readonly Dictionary<string, Func<XMLComponent>> _registry = new()
    {
        ["button"] = () => new ButtonElement(),
        ["text"]   = () => new TextElement(),
        ["div"]    = () => new DivElement(),
        ["layout"] = () => new DivElement(),
    };

    /// <summary>
    /// Registers a custom XML tag with a factory that produces its component.
    /// Call during mod startup (e.g. from a <c>Mod</c> constructor) before any layouts are parsed.
    /// </summary>
    public static void RegisterTag(string tagName, Func<XMLComponent> factory)
        => _registry[tagName] = factory;

    public static void Parse(XmlNode xmlNode, UILayoutNode target)
    {
        target.Tag = xmlNode.Name;
        target.Id = xmlNode.Attributes?["id"]?.Value;

        if (!_registry.TryGetValue(xmlNode.Name, out var factory))
        {
            Log.Warning($"[{VoidMod.ModName}] Unknown layout tag <{xmlNode.Name}>, skipping.");
            return;
        }

        XMLComponent element = factory();

        // Each element reads its own typed attributes (label, icon, color, etc.).
        element.ParseXmlAttrs(xmlNode);

        // Parse style and classes — applied in ResolveClasses after all defs load.
        if (xmlNode.Attributes?["style"]?.Value is { } inlineStyle)
            target.InlineStyle = TaffyStyleParser.ParseInlineStyle(inlineStyle);

        if (xmlNode.Attributes?["class"]?.Value is { } classAttr)
            target.UnresolvedClasses = classAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // If no classes/inline style, apply inline style directly.
        if (target.UnresolvedClasses == null)
        {
            element.Style = target.InlineStyle ?? new StyleOverride();
            target.InlineStyle = null;
        }

        // Collect non-structural attributes into Attrs for C# extensibility.
        // Element-specific attributes (label, icon, etc.) are also included — duplicating
        // them in Attrs is harmless and lets mod code read them uniformly via element.Get().
        if (xmlNode.Attributes != null)
        {
            foreach (XmlAttribute attr in xmlNode.Attributes)
            {
                if (!StructuralAttrs.Contains(attr.Name))
                    element.Attrs[attr.Name] = attr.Value;
            }
        }

        target.Props = element;

        // Parse children — skipped for leaf elements (section, text, etc.).
        if (!element.IsLeaf)
        {
            foreach (XmlNode child in xmlNode.ChildNodes)
            {
                if (child is XmlComment) continue;
                if (child is XmlText txt && txt.Value?.Trim().Length == 0) continue;

                var childNode = new UILayoutNode();
                childNode.LoadDataFromXmlCustom(child);
                // If Parse returned early (unknown tag), Props is default DivElement — skip.
                if (childNode.Tag == child.Name)
                    target.Children.Add(childNode);
            }
        }
    }
}
