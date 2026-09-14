using System.Xml;
using UnityEngine;
using Verse;
using Void.Taffy;
using static VoidComponents;
namespace Void.XML.Elements
{
    public class DivElement : XMLElement
    {
        Action<UIBranch>? builder = null;
        Action<Rect>? draw = null;
        LeafContext? context = null;

        public override void Parse(XmlNode xmlNode, string key, Func<XmlNode, Style> parseStyle, Func<XmlNode, Action<UIBranch>> parseChildren)
        {
            base.Parse(xmlNode, key, parseStyle, parseChildren);
            builder = parseChildren(xmlNode);
        }

        public override Action<UIBranch> Draw()
        {
            return b => b.Div(builder, draw, context, style, id);
        }
    }
}
