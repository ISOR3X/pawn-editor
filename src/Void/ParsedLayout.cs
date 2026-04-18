using System.Xml;
using Verse;
using Void.XMLComponents;

namespace Void;

/// <summary>
///     Immutable parsed representation of one XML element in a section layout tree.
///     Never mutated after <see cref="ResolveClasses" /> completes.
/// </summary>
public class ParsedLayout
{
    public readonly List<ParsedLayout> Children = [];
    public string? Id;

    // Inline style parsed immediately at load time; merged during ResolveClasses.
    internal StyleOverride? InlineStyle;
    public XMLComponent Props = new DivElement();
    public string Tag = string.Empty;

    // Stored during XML load; used by ResolveClasses then cleared.
    internal string[]? UnresolvedClasses;

    public void LoadDataFromXmlCustom(XmlNode xmlNode)
    {
        XMLLayoutParser.Parse(xmlNode, this);
    }

    /// <summary>
    ///     Resolves CSS class names (from <c>class="..."</c>) against loaded <see cref="StyleMapDef" />
    ///     defs and merges them with the inline style. Walks the tree recursively.
    /// </summary>
    public void ResolveClasses()
    {
        if (UnresolvedClasses is { Length: > 0 })
        {
            var classStyle = new StyleOverride();
            foreach (var className in UnresolvedClasses)
            {
                var found = false;
                foreach (var styleDef in DefDatabase<StyleMapDef>.AllDefsListForReading)
                    if (styleDef.Styles.TryGetValue(className, out var s))
                    {
                        classStyle = s.Merge(classStyle);
                        found = true;
                        break;
                    }

                if (!found)
                    Log.Warning(
                        $"[{VoidMod.ModName}] Unknown CSS class '{className}' (not found in any TaffyStyleDef).");
            }

            // Inline wins over class
            Props.Style = (InlineStyle ?? new StyleOverride()).Merge(classStyle);
            UnresolvedClasses = null;
            InlineStyle = null;
        }

        foreach (var child in Children)
            child.ResolveClasses();
    }

    /// <summary>Searches the tree for a node with the given <paramref name="id" />.</summary>
    public ParsedLayout? FindById(string id)
    {
        if (Id == id) return this;
        foreach (var child in Children)
            if (child.FindById(id) is { } found)
                return found;
        return null;
    }
}