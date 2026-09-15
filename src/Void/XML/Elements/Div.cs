using System.Xml;
using UnityEngine;
using Void.Taffy;

namespace Void.XML.Elements;

public class DivElement : XMLElement
{
    private readonly LeafContext? context = null;
    private readonly Action<Rect>? draw = null;
    private Action<UIBranch>? builder;

    public override void Parse(XmlNode xmlNode, string key, Func<XmlNode, Style> parseStyle,
        Func<XmlNode, Action<UIBranch>> parseChildren)
    {
        base.Parse(xmlNode, key, parseStyle, parseChildren);
        builder = parseChildren(xmlNode);
    }

    public override Action<UIBranch> Draw()
    {
        return b => b.Div(builder, draw, context, style, id);
    }
}
