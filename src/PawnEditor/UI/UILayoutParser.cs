using System.Xml;
using UnityEngine;
using Verse;

namespace PawnEditor;

/// <summary>
/// Parses a <c>&lt;layout&gt;</c> <see cref="XmlNode"/> into a <see cref="UILayoutNode"/> tree.
/// Called from <see cref="UILayoutNode.LoadDataFromXmlCustom"/>.
/// </summary>
internal static class UILayoutParser
{
    private static readonly HashSet<string> ReservedAttrs = ["id", "style", "class", "label",
        "icon", "color", "translate"];

    public static void Parse(XmlNode xmlNode, UILayoutNode target)
    {
        target.Tag = xmlNode.Name;
        target.Id = xmlNode.Attributes?["id"]?.Value;

        UIElement? element = xmlNode.Name switch
        {
            "button" => ParseButton(xmlNode),
            "text" => ParseText(xmlNode),
            "section" => ParseSection(xmlNode, target),
            "div" or "layout" => new DivElement(),
            _ => null,
        };

        if (element == null)
        {
            Log.Warning($"[{PawnEditorMod.ModName}] Unknown layout tag <{xmlNode.Name}>, skipping.");
            return;
        }

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

        // Collect unknown attributes into Attrs dict for C# extensibility.
        if (xmlNode.Attributes != null)
        {
            foreach (XmlAttribute attr in xmlNode.Attributes)
            {
                if (!ReservedAttrs.Contains(attr.Name))
                    element.Attrs[attr.Name] = attr.Value;
            }
        }

        target.Props = element;

        // Parse children (all tags except section and text, which are leaf nodes).
        if (xmlNode.Name is not "section" and not "text")
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

    private static ButtonElement ParseButton(XmlNode xmlNode)
    {
        var el = new ButtonElement();
        if (xmlNode.Attributes?["label"]?.Value is { } label)
            el.Label = label;
        if (xmlNode.Attributes?["icon"]?.Value is { } iconName)
            el.IconName = iconName;  // Resolved lazily in Render — textures not available at parse time.
        return el;
    }

    private static TextElement ParseText(XmlNode xmlNode)
    {
        var el = new TextElement();
        var content = xmlNode.InnerText;
        var translate = xmlNode.Attributes?["translate"]?.Value;
        el.Content = translate == "true" ? content.Translate() : content;

        if (xmlNode.Attributes?["color"]?.Value is { } colorStr)
            el.Color = ParseColor(colorStr);

        return el;
    }

    private static SectionElement ParseSection(XmlNode xmlNode, UILayoutNode target)
    {
        var el = new SectionElement();
        var defName = xmlNode.InnerText.Trim();
        if (!defName.NullOrEmpty())
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(el, nameof(SectionElement.ResolvedDef), defName);
        return el;
    }

    private static readonly Dictionary<string, Color?> ColorCache = new();

    private static Color? ParseColor(string s)
    {
        if (ColorCache.TryGetValue(s, out var cached))
            return cached;

        Color? result = null;

        // Hex or HTML named color (#RRGGBB, #RRGGBBAA, "red", "white", etc.)
        if (UnityEngine.ColorUtility.TryParseHtmlString(s, out var htmlColor))
        {
            result = htmlColor;
        }
        else if (s.Contains('.'))
        {
            // Reflection: "ColoredText.TipSectionTitleColor" → split on last dot
            var lastDot = s.LastIndexOf('.');
            var typeName = s[..lastDot];
            var fieldName = s[(lastDot + 1)..];

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(typeName) ?? asm.GetType("Verse." + typeName) ?? asm.GetType("RimWorld." + typeName);
                if (type == null) continue;
                var field = type.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field?.GetValue(null) is Color c)
                {
                    result = c;
                    break;
                }
                var prop = type.GetProperty(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (prop?.GetValue(null) is Color pc)
                {
                    result = pc;
                    break;
                }
            }

            if (result == null)
                Log.Warning($"[{PawnEditorMod.ModName}] Could not resolve color '{s}'.");
        }
        else
        {
            Log.Warning($"[{PawnEditorMod.ModName}] Unknown color format '{s}'.");
        }

        ColorCache[s] = result;
        return result;
    }
}
