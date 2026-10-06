using System.Xml;
using Verse;
using Void.Taffy;
using Void.XML.Elements;

namespace Void.XML;

/// <summary>
///     Parses an XML element into a builder that describes the same tree to a <see cref="UITree" />
///     every frame.
/// </summary>
public static class LayoutParser
{
    /// <summary>
    ///     Registry for XML elements. Key is a function that creates a new element.
    /// </summary>
    public static Dictionary<string, Func<XMLElement>> REGISTRY = [];

    static LayoutParser()
    {
        REGISTRY.Add("button", () => new ButtonElement());
        REGISTRY.Add("div", () => new DivElement());
        REGISTRY.Add("text", () => new TextElement());
    }

    public static Action<UIBranch> ParseChildren(XmlNode xmlNode)
    {
        var children = new List<Action<UIBranch>>();
        var index = 0;

        foreach (XmlNode child in xmlNode.ChildNodes)
        {
            if (child is not XmlElement) continue;
            var i = index++;
            if (!DirectXmlToObjectNew.ValidateMayRequires(
                    child.Attributes?["MayRequire"]?.Value,
                    child.Attributes?["MayRequireAnyOf"]?.Value)) continue;
            children.Add(ParseNode(child, i));
        }

        return b =>
        {
            foreach (var child in children) child(b);
        };
    }

    private static Action<UIBranch> ParseNode(XmlNode xmlNode, int index)
    {
        var key = xmlNode.Attributes?["id"]?.Value ?? $"{xmlNode.Name}[{index}]";

        if (!REGISTRY.TryGetValue(xmlNode.Name, out var factory))
            throw new LayoutParseException(xmlNode, $"unknown element. Registered: {string.Join(", ", REGISTRY.Keys)}");

        var element = factory();
        element.Parse(xmlNode, key, ParseStyle, ParseChildren);
        return element.Draw();
    }

    /// <summary>
    ///     Parses the <c>class</c> attribute against loaded <see cref="StyleMapDef" />s (later classes win)
    ///     and merges the inline <c>style</c> attribute on top. Unknown classes log a warning and are skipped.
    /// </summary>
    public static Style ParseStyle(XmlNode xmlNode)
    {
        var style = new Style();

        if (xmlNode.Attributes?["class"]?.Value is { Length: > 0 } classAttr)
            foreach (var className in classAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var classStyle = DefDatabase<StyleMapDef>.AllDefsListForReading
                    .SelectMany(def => def.Styles)
                    .Where(pair => pair.Key == className)
                    .Select(pair => pair.Value)
                    .FirstOrDefault();

                if (classStyle == null)
                {
                    Log.Warning(
                        $"[{VoidMod.ModName}] Unknown class '{className}' on <{xmlNode.Name}> (not found in any StyleMapDef).");
                    continue;
                }

                style = classStyle.Merge(style);
            }

        if (xmlNode.Attributes?["style"]?.Value is { Length: > 0 } inline)
            style = StyleParser.ParseInlineStyle(inline).Merge(style);

        return style;
    }
}

/// <summary>
///     A layout element loaded from a def.
///     Make sure to call <c>ParseAndResolveReferences</c> in <c>ResolveReferences</c> when implementing this in a
///     <c>Def</c>.
/// </summary>
public sealed class ParsedLayout
{
    public Action<UIBranch>? _builder;
    public Style? _style;
    private XmlNode? _xmlNode;

    /// <summary>
    ///     Store the xml node for future parsing.
    ///     Class attribute resolving requires the <see cref="StyleMapDef" /> to be loaded.
    /// </summary>
    public void LoadDataFromXmlCustom(XmlNode xmlNode)
    {
        _xmlNode = xmlNode;
    }

    public void ParseAndResolveReferences()
    {
        if (_builder != null) return;
        var xml = _xmlNode ??
                  throw new InvalidOperationException($"[{VoidMod.ModName}] ParsedLayout was never loaded from XML.");
        _style = LayoutParser.ParseStyle(xml);
        _builder = LayoutParser.ParseChildren(xml);
    }
}

public sealed class LayoutParseException(XmlNode node, string message)
    : Exception($"[{VoidMod.ModName}] <{node.Name}>: {message}")
{
}
