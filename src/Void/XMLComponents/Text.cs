using System.Reflection;
using System.Xml;
using UnityEngine;
using Verse;
using Void.Components;

namespace Void.XMLComponents;

public class TextElement : XMLComponent
{
    private static readonly Dictionary<string, Color?> ColorCache = new();
    public Color? Color;

    public string? Content;
    public override bool IsLeaf => true;

    public override void ParseXmlAttrs(XmlNode node)
    {
        var content = node.InnerText;
        var translate = node.Attributes?["translate"]?.Value;
        Content = translate == "true" ? content.Translate() : content;
        if (node.Attributes?["color"]?.Value is { } colorStr) Color = ParseColor(colorStr);
    }

    public override void Render(TaffyBuilder builder, Action<TaffyBuilder>? children)
    {
        builder.Text(Content ?? string.Empty, color: Color, style: Style);
    }

    private static Color? ParseColor(string s)
    {
        if (ColorCache.TryGetValue(s, out var cached))
            return cached;

        Color? result = null;

        if (s.Contains('.'))
        {
            // Reflection: "ColoredText.TipSectionTitleColor" → split on last dot, cached by full string.
            var lastDot = s.LastIndexOf('.');
            var typeName = s[..lastDot];
            var fieldName = s[(lastDot + 1)..];

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(typeName) ??
                           asm.GetType("Verse." + typeName) ?? asm.GetType("RimWorld." + typeName);
                if (type == null) continue;
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
                if (field?.GetValue(null) is Color c)
                {
                    result = c;
                    break;
                }

                var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Static);
                if (prop?.GetValue(null) is Color pc)
                {
                    result = pc;
                    break;
                }
            }

            if (result == null)
                Log.Warning($"[{VoidMod.ModName}] Could not resolve color '{s}'.");
        }
        else
        {
            Log.Warning(
                $"[{VoidMod.ModName}] Unknown color format '{s}'. Use reflection syntax: \"TypeName.FieldName\".");
        }

        ColorCache[s] = result;
        return result;
    }
}