using System.Xml;
using Verse;
using Void.Taffy;
using Void.XML;
using Void.XML.Elements;

namespace PawnEditor.v2;

class SectionElement : XMLElement
{
    private SectionWorker worker = null!;

    public override void Parse(XmlNode xmlNode, string key, Func<XmlNode, Style> parseStyle,
        Func<XmlNode, Action<UIBranch>> parseChildren)
    {
        base.Parse(xmlNode, key, parseStyle, parseChildren);

        var typeName = xmlNode.InnerText.Trim();
                var type = GenTypes.GetTypeInAnyAssembly(typeName) ?? throw new LayoutParseException(xmlNode, $"unknown type '{typeName}'.");
                if (type.IsAbstract || !typeof(SectionWorker).IsAssignableFrom(type))
                    throw new LayoutParseException(xmlNode, $"'{typeName}' is not a concrete {nameof(SectionWorker)}.");
                worker = (SectionWorker)Activator.CreateInstance(type);
    }

    public override Action<UIBranch> Draw()
    {
        return b =>
        {
            if (!b.TryInject<object>("ctx", out var ctx) || !worker.ShowFor(ctx)) return;
            b.Div(inner => worker.DoSectionContents(inner), style: style, id: id);
        };
    }
}
