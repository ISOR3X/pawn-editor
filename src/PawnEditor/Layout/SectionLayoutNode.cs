using System.Xml;
using Verse;

namespace PawnEditor.Layout;

public class SectionLayoutNode : LayoutNode<SectionDef>
{
    public string? mayRequire;

    public override bool IsActive =>
        mayRequire.NullOrEmpty() || ModLister.AllModsActiveNoSuffix(mayRequire!.Split(','));


    public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        base.LoadDataFromXmlCustom(xmlRoot);
        if (xmlRoot.Attributes?["MayRequire"]?.Value is { } nodeMayRequire
            && !ModLister.AllModsActiveNoSuffix(nodeMayRequire.Split(',')))
            return;

        foreach (XmlNode child in xmlRoot.ChildNodes)
        {
            if (child is XmlComment) continue;

            switch (child.Name)
            {
                case "section":
                {
                    var leafNode = new SectionLayoutNode
                    {
                        // Inherit if set on the section element itself
                        flexBasis = flexBasis,
                        flexGrow = flexGrow,
                    };

                    // Read attributes on the section element too
                    if (child.Attributes?["flexBasis"]?.Value is { } b)
                        leafNode.flexBasis = ParseHelper.FromString<float>(b);
                    if (child.Attributes?["flexGrow"]?.Value is { } g)
                        leafNode.flexGrow = ParseHelper.FromString<float>(g);
                    if (child.Attributes?["MayRequire"]?.Value is { } mr
                        && !ModLister.AllModsActiveNoSuffix(mr.Split(',')))
                        continue;

                    DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(leafNode, "leaf", child.InnerText.Trim());
                    children.Add(leafNode);
                    break;
                }
                case "flex":
                {
                    var childNode = DirectXmlToObject.ObjectFromXml<SectionLayoutNode>(child, true);
                    if (childNode != null)
                        children.Add(childNode);
                    break;
                }
            }
        }
    }
}