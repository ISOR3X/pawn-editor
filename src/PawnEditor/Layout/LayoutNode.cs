using System.Xml;
using Verse;

namespace PawnEditor.Layout;

public enum FlexDirection
{
    Col,
    Row
}

public class LayoutNode<TLeaf>
{
    public float flexBasis;
    public float flexGrow;

    public TLeaf? leaf;
    public bool IsLeaf => leaf != null;
    public bool IsActive = true;

    public virtual void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        if (xmlRoot.Attributes?["flexBasis"]?.Value is { } basis)
            flexBasis = ParseHelper.FromString<float>(basis);
        if (xmlRoot.Attributes?["flexGrow"]?.Value is { } grow)
            flexGrow = ParseHelper.FromString<float>(grow);

        if (xmlRoot.Attributes?["mayRequire"]?.Value is { } nodeMayRequire
            && !ModLister.AllModsActiveNoSuffix(nodeMayRequire.Split(',')))
            IsActive = false;
    }
}