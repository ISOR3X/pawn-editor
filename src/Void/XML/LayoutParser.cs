using System.Xml;
using Verse;
using Void.Taffy;

namespace Void.XML;

/// <summary>
/// Parses an XML element into a builder that describes the same tree to a <see cref="UITree"/>
/// every frame.
/// </summary>
public static class LayoutParser
{
    public static Action<UIBranch> ParseChildren(XmlNode xml)
    {
        var children = new List<Action<UIBranch>>();
        var index = 0;
        foreach (XmlNode child in xml.ChildNodes)
            if (child is XmlElement)
                children.Add(ParseNode(child, index++));

        return b => { foreach (var child in children) child(b); };
    }

    private static Action<UIBranch> ParseNode(XmlNode xml, int index)
    {
        var key = xml.Attributes?["id"]?.Value ?? $"{xml.Name}[{index}]";
        var style = ParseStyle(xml);

        if (xml.Name == "text")
        {
            var text = xml.InnerText.Trim();
            return b => b.Text(text, style, key);
        }

        var children = ParseChildren(xml);
        return b => b.Div(children, style: style, id: key);
    }

    /// <summary>
    /// Parses the <c>class</c> attribute against loaded <see cref="StyleMapDef" />s (later classes win)
    /// and merges the inline <c>style</c> attribute on top. Unknown classes log a warning and are skipped.
    /// </summary>
    public static Style ParseStyle(XmlNode xml)
    {
        var style = new Style();

        if (xml.Attributes?["class"]?.Value is { Length: > 0 } classAttr)
            foreach (var className in classAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var classStyle = DefDatabase<StyleMapDef>.AllDefsListForReading
                        .SelectMany(def => def.Styles)
                        .Where(pair => pair.Key == className)
                        .Select(pair => pair.Value)
                        .FirstOrDefault();

                if (classStyle == null)
                {
                    Log.Warning($"[{VoidMod.ModName}] Unknown class '{className}' on <{xml.Name}> (not found in any StyleMapDef).");
                    continue;
                }

                style = classStyle.Merge(style);
            }

        if (xml.Attributes?["style"]?.Value is { Length: > 0 } inline)
            style = StyleParser.ParseInlineStyle(inline).Merge(style);

        return style;
    }

}
/// <summary>
/// A layout element loaded from a def.
/// Make sure to call <c>ParseAndResolveReferences</c> in <c>ResolveReferences</c> when implementing this in a <c>Def</c>.
/// </summary>
public sealed class ParsedLayout
{
    public Action<UIBranch>? _builder = null;
    public Style? _style = null;
    private XmlNode? _xmlNode;

    /// <summary>
    /// Store the xml node for future parsing.
    /// Class attribute resolving requires the <see cref="StyleMapDef"/> to be loaded.
    /// </summary>
    public void LoadDataFromXmlCustom(XmlNode xmlNode)
    {
        _xmlNode = xmlNode;
    }

    public void ParseAndResolveReferences()
    {
        if (_builder != null) return;
        var xml = _xmlNode ?? throw new InvalidOperationException($"[{VoidMod.ModName}] ParsedLayout was never loaded from XML.");
        _style = LayoutParser.ParseStyle(xml);
        _builder = LayoutParser.ParseChildren(xml);
    }
}
