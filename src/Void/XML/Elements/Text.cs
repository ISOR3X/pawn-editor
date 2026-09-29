using System.Xml;
using Void.Taffy;

namespace Void.XML.Elements;

public class TextElement : XMLElement
{
    private string? text;

    public override void Parse(XmlNode xmlNode, string key, Func<XmlNode, Style> parseStyle,
        Func<XmlNode, Action<UIBranch>> parseChildren)
    {
        base.Parse(xmlNode, key, parseStyle, parseChildren);

        text = xmlNode.InnerText.Trim();
    }

    public override Action<UIBranch> Draw()
    {
        return b => b.Text(text ?? string.Empty, style, id);
    }
}