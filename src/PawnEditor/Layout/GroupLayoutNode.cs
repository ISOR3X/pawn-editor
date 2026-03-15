using System.Collections.Generic;
using System.Xml;
using Verse;

namespace PawnEditor.Layout;

public abstract class GroupLayoutNode<TLeaf> : LayoutNode<TLeaf>
{
    public List<LayoutNode<TLeaf>> children = [];

    public float gap = 4f;
    public float? gapX;
    public float? gapY;

    public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        base.LoadDataFromXmlCustom(xmlRoot);

        if (xmlRoot.Attributes?["gap"]?.Value is { } flexGap)
            gap = ParseHelper.FromString<float>(flexGap);
        if (xmlRoot.Attributes?["gapX"]?.Value is { } flexGapX)
            gapX = ParseHelper.FromString<float>(flexGapX);
        if (xmlRoot.Attributes?["gapY"]?.Value is { } flexGapY)
            gapY = ParseHelper.FromString<float>(flexGapY);
    }
}