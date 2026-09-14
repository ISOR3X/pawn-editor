using System.Xml;
using Void.Taffy;

namespace Void.XML.Elements
{
    public abstract class XMLElement
    {
        public Style? style = null;
        protected string? id;

        public virtual void Parse(XmlNode xmlNode, string key, Func<XmlNode, Style> parseStyle, Func<XmlNode, Action<UIBranch>> parseChildren)
        {
            id = key;
            style = parseStyle(xmlNode);
        }
        public abstract Action<UIBranch> Draw();
    }
}
