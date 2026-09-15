using System.Xml;
using Void.Taffy;

namespace Void.XML.Elements;

public abstract class XMLElement
{
    protected string? id;
    public Style? style;

    public virtual void Parse(XmlNode xmlNode, string key, Func<XmlNode, Style> parseStyle,
        Func<XmlNode, Action<UIBranch>> parseChildren)
    {
        id = key;
        style = parseStyle(xmlNode);
    }

    public abstract Action<UIBranch> Draw();
}
