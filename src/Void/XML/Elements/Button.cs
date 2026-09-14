using System.Xml;
using UnityEngine;
using Verse;
using Void.Taffy;
using static VoidComponents;

namespace Void.XML.Elements;

public class ButtonElement : XMLElement
{
    public bool block;
    public bool disabled;
    public Texture2D? icon = null;
    public Color? iconColor = null;
    public string? label;
    public Action<Rect>? onClick = null;
    public Action<Rect>? onHover = null;
    public ComponentSize size = ComponentSize.Default;
    public ButtonVariant variant = ButtonVariant.Solid;

    public override void Parse(XmlNode xmlNode, string key, Func<XmlNode, Style> parseStyle,
        Func<XmlNode, Action<UIBranch>> parseChildren)
    {
        base.Parse(xmlNode, key, parseStyle, parseChildren);
        label = xmlNode.InnerText.Trim();

        if (Enum.TryParse(xmlNode.Attributes?["size"]?.Value.CapitalizeFirst(), out ComponentSize s)) size = s;
        if (Enum.TryParse(xmlNode.Attributes?["variant"]?.Value.CapitalizeFirst(), out ButtonVariant v)) variant = v;
        if (bool.TryParse(xmlNode.Attributes?["block"]?.Value, out var bl)) block = bl;
        if (bool.TryParse(xmlNode.Attributes?["disabled"]?.Value, out var dis)) disabled = dis;
    }

    public override Action<UIBranch> Draw()
    {
        return b => b.Button(label, icon, iconColor, onClick, onHover, block, disabled, size, variant, style, id);
    }
}